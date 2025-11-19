using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Versão do schema do JSON de cenário.
/// Aumentamos para 3 porque agora salvamos prefabPath corretamente
/// e suportamos reconstrução de objetos.
/// </summary>
public static class ScenarioSchema
{
    // v1 = objects
    // v2 = artworks + skybox
    // v3 = adiciona prefabPath obrigatório para reconstrução
    public const int Current = 3;
}

/// <summary>
/// Estado de um objeto editável no cenário.
/// Posição/rotação em WORLD; escala em LOCAL.
/// </summary>
[Serializable]
public class ObjectState
{
    public string id;            // ID estável do EditableObject
    public string prefabPath;    // novo: usado para recriar objetos via PrefabRegistry

    // Transform
    public float px, py, pz;     // posição WORLD
    public float rx, ry, rz;     // rotação WORLD (Euler)
    public float sx, sy, sz;     // escala LOCAL

    // Material opcional
    public string materialId;
}

/// <summary>
/// Entrada de obra por ID do objeto.
/// </summary>
[Serializable]
public class ArtworkEntryById
{
    public string objectId;
    public string imageUrl;
}

/// <summary>
/// Preset simples de skybox.
/// </summary>
[Serializable]
public class SkyboxPreset
{
    public string type;
    public string materialName;
    public float rotation;
    public float exposureCompensation;
}

/// <summary>
/// Snapshot completo do cenário.
/// </summary>
[Serializable]
public class ScenarioData
{
    // ---- Metadados ----
    public int schemaVersion = ScenarioSchema.Current;
    public string appVersion = "";
    public string sceneBaseId = "";
    public string name = "";
    public string createdAtIso = "";

    // ---- Lista de objetos ----
    public List<ObjectState> objects = new();

    // ---- Artworks ----
    public List<ArtworkEntryById> artworks = new();

    // ---- Skybox ----
    public string skyboxMaterialName = "";
    public SkyboxPreset skyboxPreset;

    // ===========================================================
    // HELPERS
    // ===========================================================

    /// <summary>
    /// Retorna se o JSON é antigo (v1 ou v2).
    /// v3 exige prefabPath.
    /// </summary>
    public bool IsLegacy()
        => schemaVersion < ScenarioSchema.Current;

    /// <summary>
    /// Retorna o nome correto do material de skybox.
    /// </summary>
    public string GetSkyboxMaterialNameOrNull()
    {
        if (!string.IsNullOrEmpty(skyboxMaterialName))
            return skyboxMaterialName;

        if (skyboxPreset != null && !string.IsNullOrEmpty(skyboxPreset.materialName))
            return skyboxPreset.materialName;

        return null;
    }
}

/// <summary>
/// Utilitários de arredondamento.
/// </summary>
public static class ScenarioMath
{
    public static float Round(float v, int digits = 3)
        => (float)Math.Round(v, digits, MidpointRounding.AwayFromZero);

    public static float Normalize360(float degrees)
    {
        degrees %= 360f;
        if (degrees < 0f) degrees += 360f;
        return degrees;
    }
}
