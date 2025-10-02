using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ponto de entrada para carregar um JSON de cenário:
/// 1) (opcional) aplica skybox
/// 2) aplica objetos (pos/rot/scale/material)
/// 3) baixa/aplica obras (ScenarioData.artworks)
/// </summary>
public class ScenarioOrchestrator : MonoBehaviour
{
    [Header("Refs")]
    public SceneStateManager sceneStateManager;    // aplica pos/rot/scale/material
    public SkyboxApplier skyboxApplier;            // aplica skybox do JSON
    public SelectedArtworksLoader artworksLoader;  // baixa/aplica imagens

    [Header("Ordem")]
    public bool applySkyboxBeforeObjects = true;

    /// <summary>
    /// Carrega o cenário a partir de um JSON (string).
    /// </summary>
    public void LoadScenarioJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogWarning("[ScenarioOrchestrator] JSON vazio.");
            return;
        }

        ScenarioData data;
        try { data = JsonUtility.FromJson<ScenarioData>(json); }
        catch (Exception e) { Debug.LogError("[ScenarioOrchestrator] JSON inválido: " + e.Message); return; }

        LoadScenarioData(data);
    }

    /// <summary>
    /// Carrega o cenário a partir de um objeto ScenarioData já parseado.
    /// </summary>
    public void LoadScenarioData(ScenarioData data)
    {
        if (data == null) { Debug.LogWarning("[ScenarioOrchestrator] ScenarioData nulo."); return; }

        // 0) Skybox opcional antes
        if (applySkyboxBeforeObjects && skyboxApplier)
            skyboxApplier.ApplyFromScenario(data);

        // 1) Objetos (transform + materialId)
        if (sceneStateManager) sceneStateManager.Apply(data);

        // 2) Skybox depois (se preferir)
        if (!applySkyboxBeforeObjects && skyboxApplier)
            skyboxApplier.ApplyFromScenario(data);

        // 3) Obras
        if (artworksLoader != null && data.artworks != null && data.artworks.Count > 0)
        {
            var idToEditable = BuildIdIndex();
            StartCoroutine(artworksLoader.ApplyArtworksById(data.artworks, idToEditable));
        }
        else
        {
            Debug.Log("[ScenarioOrchestrator] Sem artworks no JSON (ou loader não atribuído).");
        }
    }

    // ----- helpers -----

    private IReadOnlyDictionary<string, EditableObject> BuildIdIndex()
    {
        var map = new Dictionary<string, EditableObject>(StringComparer.Ordinal);
#if UNITY_2023_1_OR_NEWER
        var all = GameObject.FindObjectsByType<EditableObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        var all = GameObject.FindObjectsOfType<EditableObject>(true);
#endif
        foreach (var eo in all)
        {
            if (eo && !string.IsNullOrEmpty(eo.Id))
                map[eo.Id] = eo;
        }
        return map;
    }
}
