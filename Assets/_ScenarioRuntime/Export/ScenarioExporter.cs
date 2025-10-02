// Em algum componente seu de export (ex.: ScenarioExporter)
using System.Collections.Generic;
using UnityEngine;

public class ScenarioExporter : MonoBehaviour
{
    public SceneStateManager sceneStateManager;
    public ArtworkIndexResolver indexResolver;   // arraste e ordene seus quadros aqui
    public string selectedArtworksKey = "selectedArtworks";
    public string defaultFileName = "Scenario.json";

    public void ExportCurrentScenario()
    {
        var data = sceneStateManager.Capture(scenarioName: "Mapa Exportado", includeMaterials: true);

        // Mescla do localStorage no formato que você mostrou:
        var metas = new List<ArtworkMeta>();
        var count = SelectedArtworksMerger_Tainacan.MergeFromLocalStorageTainacan(
            data,
            selectedArtworksKey,
            indexResolver ? indexResolver.GetResolver() : null,
            metasOut: metas,
            clearExistingArtworks: true
        );

        // (Opcional) usar 'metas' para gerar labels/placas agora ou salvar num sidecar
        // Ex.: Debug.Log($"{count} obras embutidas. Primeiro título: {metas[0].title}");

        var json = JsonUtility.ToJson(data, true);
        StorageBridge.SaveAsDownload(defaultFileName, json);
    }
}
