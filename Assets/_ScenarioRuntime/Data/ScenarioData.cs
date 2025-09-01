using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Versão do schema do JSON de cenário. Se mudar a estrutura, incremente.
/// </summary>
public static class ScenarioSchema
{
    public const int Current = 1;
}

/// <summary>
/// Estado de um objeto editável no cenário.
/// Posição/rotação em WORLD; escala em LOCAL.
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
/// Snapshot completo do cenário.
/// </summary>
[Serializable]
public class ScenarioData
{
    public int schemaVersion = ScenarioSchema.Current;
    public string appVersion = "";            // preenchido ao capturar
    public string sceneBaseId = "";           // string que identifica a cena base
    public string name = "";
    public string createdAtIso = "";          // ISO 8601 (UTC) preenchido ao capturar

    public List<ObjectState> objects = new();
}

/// <summary>
/// Utilitários de arredondamento/normalização para garantir idempotência.
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
