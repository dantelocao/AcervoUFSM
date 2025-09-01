// Assets/_ScenarioRuntime/ScenarioDebugUI.cs
using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Componente simples para debug/testes no Play Mode.
/// Mostra bot�es no Inspector para capturar/aplicar/resetar cen�rios.
/// </summary>
public class ScenarioDebugUI : MonoBehaviour
{
    public SceneStateManager manager;

#if UNITY_EDITOR
    private const string LastPathKey = "ScenarioDebugUI.LastJsonPath";

    [CustomEditor(typeof(ScenarioDebugUI))]
    public class ScenarioDebugUIEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var ui = (ScenarioDebugUI)target;
            if (ui.manager == null)
            {
                EditorGUILayout.HelpBox("Arraste o SceneStateManager aqui.", MessageType.Warning);
                return;
            }

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Ferramentas de Cen�rio (Editor)", EditorStyles.boldLabel);

            // CAPTURAR E SALVAR
            if (GUILayout.Button(" Capturar e Salvar JSON..."))
            {
                var data = ui.manager.Capture("CapturedScenario", includeMaterials: true);
                var json = ui.manager.ToJson(data, prettyPrint: true);

                var last = EditorPrefs.GetString(LastPathKey, Application.dataPath);
                var path = EditorUtility.SaveFilePanel("Salvar cen�rio JSON", last, "Scenario.json", "json");
                if (!string.IsNullOrEmpty(path))
                {
                    File.WriteAllText(path, json);
                    EditorPrefs.SetString(LastPathKey, Path.GetDirectoryName(path));
                    Debug.Log($"[ScenarioDebugUI] JSON salvo em: {path}");
                }
            }

            // APLICAR DE ARQUIVO
            if (GUILayout.Button("Aplicar JSON de Arquivo..."))
            {
                var last = EditorPrefs.GetString(LastPathKey, Application.dataPath);
                var path = EditorUtility.OpenFilePanel("Abrir cen�rio JSON", last, "json");
                if (!string.IsNullOrEmpty(path))
                {
                    try
                    {
                        var json = File.ReadAllText(path);
                        var data = ui.manager.FromJson(json);
                        if (data == null)
                        {
                            Debug.LogError("[ScenarioDebugUI] Arquivo inv�lido (n�o parseou).");
                        }
                        else
                        {
                            ui.manager.Apply(data);
                            EditorPrefs.SetString(LastPathKey, Path.GetDirectoryName(path));
                            Debug.Log($"[ScenarioDebugUI] Aplicado: {path}");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[ScenarioDebugUI] Erro ao ler/aplicar JSON: {ex.Message}");
                    }
                }
            }

            // APLICAR DO CLIPBOARD (extra)
            if (GUILayout.Button(" Aplicar JSON do Clipboard"))
            {
                var json = EditorGUIUtility.systemCopyBuffer;
                if (string.IsNullOrEmpty(json))
                {
                    Debug.LogWarning("[ScenarioDebugUI] Clipboard vazio.");
                }
                else
                {
                    var data = ui.manager.FromJson(json);
                    if (data == null)
                        Debug.LogError("[ScenarioDebugUI] Conte�do do clipboard n�o � um JSON de cen�rio v�lido.");
                    else
                    {
                        ui.manager.Apply(data);
                        Debug.Log("[ScenarioDebugUI] Aplicado JSON do clipboard.");
                    }
                }
            }

            // RESET
            if (GUILayout.Button(" Resetar para DefaultScenario"))
            {
                ui.manager.ResetToDefault();
            }

            GUILayout.Space(6);
            EditorGUILayout.HelpBox(
                "Essas a��es funcionam no Editor (Play Mode). Em WebGL faremos uma UI pr�pria.\n" +
                "Dica: use o bot�o 'Aplicar JSON de Arquivo...' para testar quaisquer snapshots.",
                MessageType.Info);
        }
    }
#endif
}
