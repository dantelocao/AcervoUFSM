// Assets/_ScenarioRuntime/Editor/ScenarioTools.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public static class ScenarioTools
{
    [MenuItem("Tools/Scenario/Capturar e Salvar DefaultScenario Asset...")]
    public static void CaptureAndSaveDefault()
    {
        var manager = Object.FindFirstObjectByType<SceneStateManager>();
        if (manager == null)
        {
            EditorUtility.DisplayDialog("SceneStateManager",
                "Nenhum SceneStateManager encontrado na cena.\n" +
                "Crie um GameObject com SceneStateManager para capturar.",
                "OK");
            return;
        }

        var data = manager.Capture("DefaultScenario", includeMaterials: true);
        var json = manager.ToJson(data, prettyPrint: true);

        var path = EditorUtility.SaveFilePanel("Salvar DefaultScenario.json",
            "Assets", "DefaultScenario.json", "json");
        if (string.IsNullOrEmpty(path)) return;

        File.WriteAllText(path, json);
        Debug.Log($"DefaultScenario salvo em: {path}");

        // Opcional: importar como TextAsset dentro do projeto, se for dentro de Assets
        var projPath = path.Replace(Application.dataPath, "Assets");
        if (projPath.StartsWith("Assets"))
        {
            AssetDatabase.ImportAsset(projPath, ImportAssetOptions.ForceUpdate);
            var ta = AssetDatabase.LoadAssetAtPath<TextAsset>(projPath);
            if (ta != null)
            {
                // tenta preencher automaticamente no manager (se o asset estiver dentro do projeto)
                Undo.RecordObject(manager, "Set DefaultScenarioJson");
                manager.defaultScenarioJson = ta;
                EditorUtility.SetDirty(manager);
                Debug.Log("DefaultScenario TextAsset atribuído no SceneStateManager.");
            }
        }
        else
        {
            EditorUtility.DisplayDialog("Aviso",
                "O arquivo foi salvo fora da pasta Assets.\n" +
                "Se quiser usá-lo como TextAsset, mova-o para dentro de Assets/",
                "OK");
        }
    }
}
#endif
