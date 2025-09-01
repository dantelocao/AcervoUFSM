// Assets/_ScenarioRuntime/SceneStateManager.cs
using System;
using System.Linq;
using UnityEngine;

public class SceneStateManager : MonoBehaviour
{
    [Header("Identidade da cena")]
    [Tooltip("Identificador da cena base (ex.: 'Showroom_A') para validar JSONs.")]
    public string sceneBaseId = "DefaultScene";

    [Header("Default Scenario (para Reset)")]
    [Tooltip("JSON do estado padrão, empacotado como TextAsset.")]
    public TextAsset defaultScenarioJson;

    [Header("Limites e Snapping")]
    public bool useBounds = false;
    public Vector3 boundsCenter = Vector3.zero;
    public Vector3 boundsSize = new Vector3(20, 10, 20);
    [Space(4)]
    public bool clampY = true;
    public float yMin = 0f;
    public float yMax = 5f;
    [Space(4)]
    public bool snapPosition = false;
    public float positionStep = 0.25f;
    public bool snapRotation = false;
    public float rotationStep = 15f;
    public bool clampScale = true;
    public float scaleMin = 0.5f;
    public float scaleMax = 2.0f;

    Bounds BoundsAABB => new Bounds(boundsCenter, boundsSize);

    // -----------------------
    // CAPTURA (Cena -> JSON)
    // -----------------------
    public ScenarioData Capture(string scenarioName = "MyScenario", bool includeMaterials = true)
    {
        var data = new ScenarioData
        {
            schemaVersion = ScenarioSchema.Current,
            appVersion = Application.version,
            sceneBaseId = sceneBaseId,
            name = scenarioName,
            createdAtIso = DateTime.UtcNow.ToString("o")
        };

        var all = FindAllEditable();
        // Ordem determinística por Id
        foreach (var o in all.OrderBy(e => e.Id))
        {
            if (o == null) continue;
            var t = o.transform;

            var euler = t.rotation.eulerAngles;
            var st = new ObjectState
            {
                id = o.Id,
                px = ScenarioMath.Round(t.position.x),
                py = ScenarioMath.Round(t.position.y),
                pz = ScenarioMath.Round(t.position.z),

                rx = ScenarioMath.Round(ScenarioMath.Normalize360(euler.x)),
                ry = ScenarioMath.Round(ScenarioMath.Normalize360(euler.y)),
                rz = ScenarioMath.Round(ScenarioMath.Normalize360(euler.z)),

                sx = ScenarioMath.Round(t.localScale.x),
                sy = ScenarioMath.Round(t.localScale.y),
                sz = ScenarioMath.Round(t.localScale.z)
            };

            if (includeMaterials)
            {
                var mr = o.GetComponentInChildren<MeshRenderer>();
                if (mr && mr.sharedMaterial && MaterialRegistry.Instance != null)
                    st.materialId = MaterialRegistry.Instance.GetId(mr.sharedMaterial);
            }

            data.objects.Add(st);
        }

        return data;
    }

    // -----------------------
    // APLICAÇÃO (JSON -> Cena)
    // -----------------------
    public void Apply(ScenarioData data)
    {
        if (data == null) { Debug.LogWarning("[SceneStateManager] ScenarioData nulo."); return; }

        if (data.schemaVersion != ScenarioSchema.Current)
        {
            Debug.LogError($"[SceneStateManager] schemaVersion incompatível. Esperado {ScenarioSchema.Current}, recebido {data.schemaVersion}.");
            return;
        }

        if (!string.IsNullOrEmpty(data.sceneBaseId) && data.sceneBaseId != sceneBaseId)
        {
            Debug.LogWarning($"[SceneStateManager] sceneBaseId diferente (json '{data.sceneBaseId}' vs manager '{sceneBaseId}'). Aplicando com cautela.");
        }

        var all = FindAllEditable();
        foreach (var st in data.objects)
        {
            var obj = all.FirstOrDefault(o => o.Id == st.id);
            if (obj == null)
            {
                Debug.LogWarning($"[SceneStateManager] Objeto id '{st.id}' não encontrado na cena. Ignorando.");
                continue;
            }

            // POSIÇÃO (WORLD)
            var pos = new Vector3(st.px, st.py, st.pz);
            pos = ClampPosition(pos);
            pos = SnapPosition(pos);
            obj.transform.position = pos;

            // ROTAÇÃO (WORLD)
            var eul = new Vector3(
                ScenarioMath.Normalize360(st.rx),
                ScenarioMath.Normalize360(st.ry),
                ScenarioMath.Normalize360(st.rz)
            );
            eul = SnapRotation(eul);
            obj.transform.rotation = Quaternion.Euler(eul);

            // ESCALA (LOCAL)
            var scl = new Vector3(st.sx, st.sy, st.sz);
            scl = ClampScale(scl);
            obj.transform.localScale = scl;

            // MATERIAL (opcional)
            if (!string.IsNullOrEmpty(st.materialId) && MaterialRegistry.Instance != null)
            {
                var mr = obj.GetComponentInChildren<MeshRenderer>();
                var mat = MaterialRegistry.Instance.Get(st.materialId);
                if (mr && mat) mr.sharedMaterial = mat;
            }
        }
    }

    // -----------------------
    // RESET (DefaultScenario)
    // -----------------------
    public void ResetToDefault()
    {
        if (defaultScenarioJson == null)
        {
            Debug.LogWarning("[SceneStateManager] defaultScenarioJson não atribuído.");
            return;
        }
        var data = JsonUtility.FromJson<ScenarioData>(defaultScenarioJson.text);
        Apply(data);
    }

    // -----------------------
    // Utilidades
    // -----------------------
    public string ToJson(ScenarioData data, bool prettyPrint = true)
        => JsonUtility.ToJson(data, prettyPrint);

    public ScenarioData FromJson(string json)
        => string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<ScenarioData>(json);

    private EditableObject[] FindAllEditable()
    {
        // Unity 6000: método recomendado
#if UNITY_2023_1_OR_NEWER
        return GameObject.FindObjectsByType<EditableObject>(FindObjectsInactive.Include,
                                                            FindObjectsSortMode.None);
#else
        return GameObject.FindObjectsOfType<EditableObject>(true);
#endif
    }

    private Vector3 ClampPosition(Vector3 p)
    {
        if (useBounds)
        {
            var b = BoundsAABB;
            var min = b.min; var max = b.max;
            p.x = Mathf.Clamp(p.x, min.x, max.x);
            p.z = Mathf.Clamp(p.z, min.z, max.z);
        }
        if (clampY) p.y = Mathf.Clamp(p.y, yMin, yMax);
        return p;
    }

    private Vector3 SnapPosition(Vector3 p)
    {
        if (!snapPosition || positionStep <= 0f) return p;
        p.x = Mathf.Round(p.x / positionStep) * positionStep;
        p.y = Mathf.Round(p.y / positionStep) * positionStep;
        p.z = Mathf.Round(p.z / positionStep) * positionStep;
        return p;
    }

    private Vector3 SnapRotation(Vector3 eul)
    {
        if (!snapRotation || rotationStep <= 0f) return eul;
        eul.x = Mathf.Round(eul.x / rotationStep) * rotationStep;
        eul.y = Mathf.Round(eul.y / rotationStep) * rotationStep;
        eul.z = Mathf.Round(eul.z / rotationStep) * rotationStep;
        return new Vector3(
            ScenarioMath.Normalize360(eul.x),
            ScenarioMath.Normalize360(eul.y),
            ScenarioMath.Normalize360(eul.z)
        );
    }

    private Vector3 ClampScale(Vector3 s)
    {
        if (!clampScale) return s;
        s.x = Mathf.Clamp(s.x, scaleMin, scaleMax);
        s.y = Mathf.Clamp(s.y, scaleMin, scaleMax);
        s.z = Mathf.Clamp(s.z, scaleMin, scaleMax);
        return s;
    }
}
