using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ScenarioIO : MonoBehaviour
{
    [Tooltip("Identificador estável da cena base")]
    public string sceneBaseId;

    [Tooltip("Registry global de materiais")]
    public MaterialRegistry materialRegistry;

    [Tooltip("Registry global de prefabs")]
    public PrefabRegistry prefabRegistry;

    [Tooltip("Parent onde objetos spawnados serão colocados")]
    public Transform objetosInstanciadosRoot;

    private const string ProxyBase = "https://projetosoftware2-ufsm.vercel.app/api/img?url=";

    private static string Proxied(string url)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return ProxyBase + Uri.EscapeDataString(url);
#else
        return url;
#endif
    }

    // =====================================================================
    // CAPTURE
    // =====================================================================
    public ScenarioData Capture()
    {
        var data = new ScenarioData
        {
            schemaVersion = ScenarioSchema.Current,
            appVersion = Application.version,
            sceneBaseId = sceneBaseId,
            name = $"Snapshot {DateTime.UtcNow:yyyy-MM-ddTHH-mm-ssZ}",
            createdAtIso = DateTime.UtcNow.ToString("o"),
            objects = new List<ObjectState>(),
            artworks = new List<ArtworkEntryById>()
        };

        foreach (var editable in FindObjectsOfType<EditableObject>(true))
        {
            var t = editable.transform;

            var state = new ObjectState
            {
                id = editable.Id,
                prefabPath = editable.PrefabPath,   //  FIX: salva mesmo se vazio
                px = ScenarioMath.Round(t.position.x),
                py = ScenarioMath.Round(t.position.y),
                pz = ScenarioMath.Round(t.position.z),
                rx = ScenarioMath.Round(ScenarioMath.Normalize360(t.eulerAngles.x)),
                ry = ScenarioMath.Round(ScenarioMath.Normalize360(t.eulerAngles.y)),
                rz = ScenarioMath.Round(ScenarioMath.Normalize360(t.eulerAngles.z)),
                sx = ScenarioMath.Round(t.localScale.x),
                sy = ScenarioMath.Round(t.localScale.y),
                sz = ScenarioMath.Round(t.localScale.z)
            };

            // MATERIAL
            var er = editable.GetComponent<EditableRenderer>();
            if (er != null)
            {
                var r = er.GetComponentInChildren<Renderer>(true);
                if (r && materialRegistry != null)
                    state.materialId = materialRegistry.GetId(r.sharedMaterial);
            }

            data.objects.Add(state);
        }

        // ARTWORKS
        foreach (var info in FindObjectsOfType<ArtworkInfo>(true))
        {
            var eo = info.GetComponent<EditableObject>();
            if (eo == null) continue;

            if (!string.IsNullOrEmpty(info.imageUrl))
            {
                data.artworks.Add(new ArtworkEntryById
                {
                    objectId = eo.Id,
                    imageUrl = info.imageUrl
                });
            }
        }

        return data;
    }

    // =====================================================================
    // APPLY
    // =====================================================================
    public void Apply(ScenarioData data)
    {
        if (data == null) return;

        var incoming = new Dictionary<string, ObjectState>(StringComparer.Ordinal);
        foreach (var o in data.objects)
            if (!string.IsNullOrEmpty(o.id))
                incoming[o.id] = o;

        // ---------------------------------------------------------
        // DESPAWN Só objetos SPAWNADOS (prefabPath  vazio)
        // ---------------------------------------------------------
        foreach (var existing in FindObjectsOfType<EditableObject>(true))
        {
            // FIXO DA CENA NÃO destruir
            if (string.IsNullOrEmpty(existing.PrefabPath))
                continue;

            // Spawnado mas não está no JSON  destruir
            if (!incoming.ContainsKey(existing.Id))
                Destroy(existing.gameObject);
        }

        // ---------------------------------------------------------
        // SPAWN / UPDATE
        // ---------------------------------------------------------
        foreach (var st in data.objects)
        {
            EditableObject obj = FindEditableById(st.id);

            if (obj == null)
            {
                // Objeto SPAWNADO (só se tiver prefabPath)
                if (!string.IsNullOrEmpty(st.prefabPath))
                {
                    var prefab = prefabRegistry.GetPrefab(st.prefabPath);
                    if (prefab == null)
                    {
                        Debug.LogWarning($"[ScenarioIO] Prefab '{st.prefabPath}' não encontrado no PrefabRegistry.");
                        continue;
                    }

                    var inst = Instantiate(prefab, objetosInstanciadosRoot);

                    obj = inst.GetComponent<EditableObject>();
                    if (obj == null)
                        obj = inst.AddComponent<EditableObject>();

                    obj.SetPrefabPath(st.prefabPath);

                    // garante que SEMPRE haverá EditableRenderer
                    var er = inst.GetComponent<EditableRenderer>();
                    if (er == null)
                        er = inst.AddComponent<EditableRenderer>();

                    // Forçar ID
                    typeof(EditableObject)
                        .GetField("id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        .SetValue(obj, st.id);
                }
                else
                {
                    // FIXO DA CENA QUE NÃO FOI ENCONTRADO? (não deve acontecer)
                    Debug.LogWarning($"[ScenarioIO] Objeto fixo com ID '{st.id}' não encontrado na cena.");
                    continue;
                }
            }

            // APPLY Transform
            obj.transform.position = new Vector3(st.px, st.py, st.pz);
            obj.transform.rotation = Quaternion.Euler(st.rx, st.ry, st.rz);
            obj.transform.localScale = new Vector3(st.sx, st.sy, st.sz);

            // APPLY Material
            if (!string.IsNullOrEmpty(st.materialId))
            {
                var er = obj.GetComponent<EditableRenderer>();
                if (er != null)
                    er.ApplyById(st.materialId);
            }
        }

        // ---------------------------------------------------------
        // APPLY ARTWORKS
        // ---------------------------------------------------------
        foreach (var art in data.artworks)
        {
            EditableObject eo = FindEditableById(art.objectId);
            if (eo == null) continue;

            var slot = eo.GetComponentInChildren<ArtworkSlot>();
            if (slot == null || !slot.IsValid) continue;

            var info = eo.GetComponent<ArtworkInfo>();
            if (info == null) info = eo.gameObject.AddComponent<ArtworkInfo>();
            info.imageUrl = art.imageUrl;

            StartCoroutine(ApplyImageCoroutine(art.imageUrl, slot.TargetRenderer));
        }

        if (SceneStateManager.Instance != null)
            SceneStateManager.Instance.CurrentData = data;
    }

    private EditableObject FindEditableById(string id)
    {
        foreach (var e in FindObjectsOfType<EditableObject>(true))
            if (e.Id == id)
                return e;

        return null;
    }

    // =====================================================================
    // TEXTURAS
    // =====================================================================
    private IEnumerator ApplyImageCoroutine(string url, Renderer renderer)
    {
        if (string.IsNullOrEmpty(url) || renderer == null)
            yield break;

        string finalUrl = Proxied(url);

        using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(finalUrl))
        {
            req.timeout = 30;
            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                Debug.LogError("[ScenarioIO] Erro ao baixar imagem: " + req.error);
                yield break;
            }

            Texture2D tex = DownloadHandlerTexture.GetContent(req);
            renderer.material.mainTexture = tex;
        }
    }
}
