// Assets/_ScenarioRuntime/ScenarioIO.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public class ScenarioIO : MonoBehaviour
{
    [Tooltip("Identificador estável da cena base")]
    public string sceneBaseId;

    [Tooltip("Registry global de materiais")]
    public MaterialRegistry materialRegistry;

    public ScenarioData Capture()
    {
        var data = new ScenarioData
        {
            schemaVersion = ScenarioSchema.Current,
            appVersion = Application.version,
            sceneBaseId = sceneBaseId,
            name = $"Snapshot {DateTime.UtcNow:yyyy-MM-ddTHH-mm-ssZ}",
            createdAtIso = DateTime.UtcNow.ToString("o"),
            objects = new List<ObjectState>()
        };

        foreach (var editable in FindObjectsOfType<EditableObject>(true))
        {
            var t = editable.transform;
            var state = new ObjectState
            {
                id = editable.Id,
                px = ScenarioMath.Round(t.position.x),
                py = ScenarioMath.Round(t.position.y),
                pz = ScenarioMath.Round(t.position.z),
                rx = ScenarioMath.Round(ScenarioMath.Normalize360(t.eulerAngles.x)),
                ry = ScenarioMath.Round(ScenarioMath.Normalize360(t.eulerAngles.y)),
                rz = ScenarioMath.Round(ScenarioMath.Normalize360(t.eulerAngles.z)),
                sx = ScenarioMath.Round(t.localScale.x),
                sy = ScenarioMath.Round(t.localScale.y),
                sz = ScenarioMath.Round(t.localScale.z),
            };

            // pega materialId se houver EditableRenderer
            var er = editable.GetComponent<EditableRenderer>();
            if (er != null)
            {
                // garante que esteja sincronizado com o registry
                var r = er.GetComponentInChildren<Renderer>(true);
                if (r && materialRegistry != null)
                {
                    var id = materialRegistry.GetId(r.sharedMaterial);
                    state.materialId = id;
                }
            }

            data.objects.Add(state);
        }

        return data;
    }

    public void Apply(ScenarioData data)
    {
        // cria índice por id
        var byId = new Dictionary<string, ObjectState>(StringComparer.Ordinal);
        foreach (var o in data.objects) if (!string.IsNullOrEmpty(o.id)) byId[o.id] = o;

        foreach (var editable in FindObjectsOfType<EditableObject>(true))
        {
            if (!byId.TryGetValue(editable.Id, out var st)) continue;

            var t = editable.transform;
            t.position = new Vector3(st.px, st.py, st.pz);
            t.rotation = Quaternion.Euler(st.rx, st.ry, st.rz);
            t.localScale = new Vector3(st.sx, st.sy, st.sz);

            if (!string.IsNullOrEmpty(st.materialId))
            {
                var er = editable.GetComponent<EditableRenderer>();
                if (er != null) er.ApplyById(st.materialId);
            }
        }
    }
}
