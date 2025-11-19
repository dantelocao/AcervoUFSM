using System;
using System.Linq;
using UnityEngine;

public class SceneStateManager : MonoBehaviour
{
    public static SceneStateManager Instance { get; private set; }

    // Apenas referência ao estado atual (quem gerencia é o ScenarioIO)
    public ScenarioData CurrentData { get; set; }

    [Header("Identidade da cena")]
    public string sceneBaseId = "DefaultScene";

    [Header("Default Scenario (para Reset)")]
    public TextAsset defaultScenarioJson;

    private void Awake()
    {
        Instance = this;

        // CurrentData começa vazio — ScenarioIO irá preencher quando aplicar/capturar
        CurrentData = new ScenarioData
        {
            schemaVersion = ScenarioSchema.Current,
            objects = new System.Collections.Generic.List<ObjectState>(),
            artworks = new System.Collections.Generic.List<ArtworkEntryById>()
        };
    }

    // =====================================================================
    // BUSCA DE OBJETOS EDITÁVEIS
    // =====================================================================

    public EditableObject[] FindAllEditable()
    {
#if UNITY_2023_1_OR_NEWER
        return GameObject.FindObjectsByType<EditableObject>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        return GameObject.FindObjectsOfType<EditableObject>(true);
#endif
    }

    public EditableObject FindObjectById(string id)
    {
        return FindAllEditable().FirstOrDefault(o => o.Id == id);
    }

    // =====================================================================
    // RESET PARA DEFAULT
    // =====================================================================

    public void ResetToDefault()
    {
        if (defaultScenarioJson == null)
        {
            Debug.LogWarning("[SceneStateManager] defaultScenarioJson não atribuído.");
            return;
        }

        var data = JsonUtility.FromJson<ScenarioData>(defaultScenarioJson.text);
        if (data == null)
        {
            Debug.LogError("[SceneStateManager] defaultScenarioJson é inválido.");
            return;
        }

        var io = FindObjectOfType<ScenarioIO>();
        if (io == null)
        {
            Debug.LogError("[SceneStateManager] Nenhum ScenarioIO encontrado na cena.");
            return;
        }

        io.Apply(data);
        CurrentData = data;
    }
}
