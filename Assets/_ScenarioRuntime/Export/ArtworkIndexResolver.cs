using System;
using System.Collections.Generic;
using UnityEngine;

public class ArtworkIndexResolver : MonoBehaviour
{
    [Tooltip("Ordem dos quadros que correspondem ao array salvo no localStorage.")]
    public List<EditableObject> orderedArtworkObjects = new();

    /// Retorna um delegate index->objectId para o exporter/merger.
    public Func<int, string> GetResolver()
    {
        return i =>
        {
            if (i < 0 || i >= orderedArtworkObjects.Count) return null;
            var eo = orderedArtworkObjects[i];
            return eo ? eo.Id : null;
        };
    }
}
