// Assets/_ScenarioRuntime/MaterialRegistry.cs
using System.Collections.Generic;
using UnityEngine;

public class MaterialRegistry : MonoBehaviour
{
    public static MaterialRegistry Instance { get; private set; }

    [System.Serializable]
    public class Entry { public string id; public Material material; }

    [Tooltip("Mapeia IDs estáveis para materiais empacotados na build.")]
    public List<Entry> entries = new();

    private readonly Dictionary<string, Material> _byId = new();
    private readonly Dictionary<Material, string> _byMat = new();

    private void Awake()
    {
        Instance = this;
        _byId.Clear(); _byMat.Clear();
        foreach (var e in entries)
        {
            if (e == null || e.material == null || string.IsNullOrEmpty(e.id)) continue;
            _byId[e.id] = e.material;
            if (!_byMat.ContainsKey(e.material)) _byMat[e.material] = e.id;
        }
    }

    public Material Get(string id) =>
        (id != null && _byId.TryGetValue(id, out var m)) ? m : null;

    public string GetId(Material m) =>
        (m != null && _byMat.TryGetValue(m, out var id)) ? id : null;
}
