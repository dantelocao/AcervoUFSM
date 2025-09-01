// Assets/_ScenarioRuntime/ScenarioDebugUI.cs
using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Componente simples para debug/testes no Play Mode.
/// Mostra botões no Inspector para capturar/aplicar/resetar cenários.
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
            EditorGUILayout.LabelField("Ferramentas de Cenário (Editor)", EditorStyles.boldLabel);

            // CAPTURAR E SALVAR
            if (GUILayout.Button(" Capturar e Salvar JSON..."))
            {
                var data = ui.manager.Capture("CapturedScenario", includeMaterials: true);
                var json = ui.manager.ToJson(data, prettyPrint: true);

                var last = EditorPrefs.GetString(LastPathKey, Application.dataPath);
                var path = EditorUtility.SaveFilePanel("Salvar cenário JSON", last, "Scenario.json", "json");
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
                var path = EditorUtility.OpenFilePanel("Abrir cenário JSON", last, "json");
                if (!string.IsNullOrEmpty(path))
                {
                    try
                    {
                        var json = File.ReadAllText(path);
                        var data = ui.manager.FromJson(json);
                        if (data == null)
                        {
                            Debug.LogError("[ScenarioDebugUI] Arquivo inválido (não parseou).");
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
                        Debug.LogError("[ScenarioDebugUI] Conteúdo do clipboard não é um JSON de cenário válido.");
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
                "Essas ações funcionam no Editor (Play Mode). Em WebGL faremos uma UI própria.\n" +
                "Dica: use o botão 'Aplicar JSON de Arquivo...' para testar quaisquer snapshots.",
                MessageType.Info);
        }
    }
#endif
}
