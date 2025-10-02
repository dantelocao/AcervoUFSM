using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Versão do schema do JSON de cenário. Incremente sempre que mudar a estrutura.
/// </summary>
public static class ScenarioSchema
{
    // v1: apenas "objects"
    // v2: adiciona "artworks" (por objectId) e "skybox" (materialName/preset)
    public const int Current = 2;
}

/// <summary>
/// Estado de um objeto editável no cenário.
/// Posição/rotação em WORLD; escala em LOCAL.
/// Compatível com o seu formato atual (v1).
/// </summary>
[Serializable]
public class ObjectState
{
    public string id;   // ID estável do objeto (EditableObject.Id)

    // Posição (WORLD)
    public float px, py, pz;

    // Rotação (WORLD, Euler em graus)
    public float rx, ry, rz;

    // Escala (LOCAL)
    public float sx, sy, sz;

    // Opcional (v1): material (mapeado via catálogo/registry)
    public string materialId;
}

/// <summary>
/// Entrada de obra por ID do objeto em cena que receberá a textura.
/// </summary>
[Serializable]
public class ArtworkEntryById
{
    public string objectId;  // deve bater com ObjectState.id / EditableObject.Id
    public string imageUrl;  // URL absoluta (no WebGL passará pelo proxy)
}

/// <summary>
/// Preset opcional de skybox. Use como preferir (material + rotação/exposure).
/// </summary>
[Serializable]
public class SkyboxPreset
{
    // Ex.: "Material", "Procedural", "Cubemap" — use como chave semântica se precisar
    public string type;

    // Se type == "Material", resolvido via MaterialRegistry (opcional)
    public string materialName;

    // Se o shader do skybox suportar _Rotation
    public float rotation;

    // Se você controlar exposição via Volume (URP/HDRP), este campo serve de ponte;
    // a aplicação prática fica por conta do seu sistema de volumes.
    public float exposureCompensation;
}

/// <summary>
/// Snapshot completo do cenário.
/// v1 compatível (objects).
/// v2 inclui artworks e skybox.
/// </summary>
[Serializable]
public class ScenarioData
{
    // --- Metadados do snapshot ---
    public int schemaVersion = ScenarioSchema.Current;
    public string appVersion = "";            // preenchido ao capturar
    public string sceneBaseId = "";           // string que identifica a cena base
    public string name = "";
    public string createdAtIso = "";          // ISO 8601 (UTC) preenchido ao capturar

    // --- Objetos (v1) ---
    public List<ObjectState> objects = new();

    // --- Novos campos (v2) ---
    // Lista de obras por ID do objeto (quadro) que receberá a imagem
    public List<ArtworkEntryById> artworks = new();

    // Skybox simples por material (via MaterialRegistry)
    public string skyboxMaterialName = "";

    // Ou preset mais completo (opcional)
    public SkyboxPreset skyboxPreset;

    // ==== Helpers (opcionais, úteis para orquestração) ====

    /// <summary>
    /// Retorna true se o JSON aparenta ser de schema v1 (sem artworks/skybox).
    /// </summary>
    public bool IsLegacyV1()
        => schemaVersion <= 1 && (artworks == null || artworks.Count == 0)
                             && string.IsNullOrEmpty(skyboxMaterialName)
                             && skyboxPreset == null;

    /// <summary>
    /// Tenta obter o "nome de material" do skybox a partir do preset ou do campo simples.
    /// </summary>
    public string GetSkyboxMaterialNameOrNull()
    {
        if (!string.IsNullOrEmpty(skyboxMaterialName)) return skyboxMaterialName;
        if (skyboxPreset != null && !string.IsNullOrEmpty(skyboxPreset.materialName))
            return skyboxPreset.materialName;
        return null;
    }
}

/// <summary>
/// Utilitários de arredondamento/normalização para garantir idempotência.
/// (mantém sua implementação atual)
/// </summary>
public static class ScenarioMath
{
    /// <summary>Arredonda float a N casas (default 3).</summary>
    public static float Round(float v, int digits = 3)
        => (float)Math.Round(v, digits, MidpointRounding.AwayFromZero);

    /// <summary>Normaliza graus para [0,360).</summary>
    public static float Normalize360(float degrees)
    {
        degrees %= 360f;
        if (degrees < 0f) degrees += 360f;
        return degrees;
    }
}
