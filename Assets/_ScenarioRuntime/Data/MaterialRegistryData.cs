using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MaterialRegistryData", menuName = "Scriptable Objects/MaterialRegistryData")]
public class MaterialRegistryData : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string id;          // ex.: "madeira_rustica"
        public Material material;  // use URP/Lit ou seu shader padrão
    }

    public List<Entry> entries = new();

}
