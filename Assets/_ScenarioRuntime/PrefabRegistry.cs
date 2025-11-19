using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "PrefabRegistry",
    menuName = "Scenario/Prefab Registry"
)]
public class PrefabRegistry : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string id;         // ex: "obra", "parede", "cubo"
        public GameObject prefab; // referência ao prefab no Project
    }

    public List<Entry> items = new List<Entry>();

    private Dictionary<string, GameObject> lookup;

    private void OnEnable()
    {
        BuildLookup();
    }

    private void BuildLookup()
    {
        lookup = new Dictionary<string, GameObject>(StringComparer.Ordinal);

        foreach (var e in items)
        {
            if (e == null || string.IsNullOrWhiteSpace(e.id) || e.prefab == null)
                continue;

            if (!lookup.ContainsKey(e.id))
                lookup.Add(e.id, e.prefab);
        }
    }

    /// <summary> Retorna prefab pelo ID textual. </summary>
    public GameObject GetPrefab(string id)
    {
        if (lookup == null || lookup.Count == 0)
            BuildLookup();

        if (string.IsNullOrEmpty(id))
            return null;

        lookup.TryGetValue(id, out var prefab);
        return prefab;
    }

    /// <summary> Verifica se existe um ID registrado. </summary>
    public bool Contains(string id)
    {
        if (lookup == null || lookup.Count == 0)
            BuildLookup();

        return lookup.ContainsKey(id);
    }
}
