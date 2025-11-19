using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class MultiFolderPrefabLoader : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown folderDropdown;
    [SerializeField] private TMP_Dropdown prefabDropdown;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private RuntimeSelectorMover_Input selector;

    [Header("Prefab Registry (para salvar prefabPath)")]
    public PrefabRegistry prefabRegistry;

    private Dictionary<string, List<GameObject>> folderPrefabs = new Dictionary<string, List<GameObject>>();
    private List<GameObject> prefabInstances = new List<GameObject>();

    private string selectedFolder = null;
    private int selectedPrefabIndex = -1;

    private void Start()
    {
        string[] folders = new string[]
        {
            "VertexModeler/LevelDesignStarterPack/Prefabs/Door",
            "VertexModeler/LevelDesignStarterPack/Prefabs/Floor",
            "VertexModeler/LevelDesignStarterPack/Prefabs/Roof",
            "VertexModeler/LevelDesignStarterPack/Prefabs/Wall",
            "VertexModeler/LevelDesignStarterPack/Prefabs/Window",
            "Prefabs/Props"
        };

        folderPrefabs.Clear();

        foreach (string folder in folders)
        {
            GameObject[] loadedPrefabs = Resources.LoadAll<GameObject>(folder);
            string folderName = folder.Substring(folder.LastIndexOf('/') + 1);

            if (!folderPrefabs.ContainsKey(folderName))
                folderPrefabs[folderName] = new List<GameObject>();

            foreach (GameObject prefab in loadedPrefabs)
                folderPrefabs[folderName].Add(prefab);
        }

        folderDropdown.ClearOptions();
        folderDropdown.AddOptions(new List<string>(folderPrefabs.Keys));
        folderDropdown.onValueChanged.AddListener(OnFolderChanged);

        prefabDropdown.ClearOptions();
        prefabDropdown.onValueChanged.AddListener(OnPrefabChanged);

        confirmButton.onClick.AddListener(OnConfirmClick);
        deleteButton.onClick.AddListener(DeleteSelected);
    }

    private void OnFolderChanged(int index)
    {
        selectedFolder = folderDropdown.options[index].text;
        UpdatePrefabDropdown();
    }

    private void UpdatePrefabDropdown()
    {
        prefabDropdown.ClearOptions();
        selectedPrefabIndex = -1;

        if (selectedFolder != null && folderPrefabs.ContainsKey(selectedFolder))
        {
            List<string> prefabNames = new List<string>();
            foreach (GameObject prefab in folderPrefabs[selectedFolder])
                prefabNames.Add(prefab.name);

            prefabDropdown.AddOptions(prefabNames);

            //  FORÇA SELECIONAR O PRIMEIRO ITEM AUTOMATICAMENTE
            if (prefabNames.Count > 0)
            {
                prefabDropdown.SetValueWithoutNotify(0);
                OnPrefabChanged(0);
            }
        }
    }


    private void OnPrefabChanged(int index)
    {
        selectedPrefabIndex = index;
    }

    void OnConfirmClick()
    {
        if (selectedFolder != null &&
            folderPrefabs.ContainsKey(selectedFolder) &&
            selectedPrefabIndex >= 0)
        {
            GameObject prefab = folderPrefabs[selectedFolder][selectedPrefabIndex];

            float offset = 2f;
            Vector3 spawnPos = spawnPoint.position + spawnPoint.forward * offset;

            GameObject newInstance = Instantiate(prefab, spawnPos, spawnPoint.rotation);
            prefabInstances.Add(newInstance);

            // GARANTE EditableObject + EditableRenderer
            var editable = newInstance.GetComponent<EditableObject>();
            if (editable == null)
                editable = newInstance.AddComponent<EditableObject>();

            if (!newInstance.TryGetComponent<EditableRenderer>(out var er))
                er = newInstance.AddComponent<EditableRenderer>();

            // ==== AQUI É O MAIS IMPORTANTE ====
            // Definir o prefabPath correto baseado no nome do prefab
            string id = prefab.name;   // <<< ID usado no registry
            editable.SetPrefabPath(id);

            // Pode manter o nome no hierarchy
            newInstance.name = prefab.name;
        }
    }


    // procura no registry pelo prefab
    private string FindPrefabIdInRegistry(GameObject prefab)
    {
        if (prefabRegistry == null)
            return null;

        foreach (var e in prefabRegistry.items)
        {
            if (e.prefab == prefab)
                return e.id;
        }

        return null;
    }

    public void DeleteSelected()
    {
        if (selector == null || selector.Current == null)
        {
            Debug.LogWarning("Nenhum objeto selecionado para deletar.");
            return;
        }

        GameObject target = selector.Current.gameObject;

        if (prefabInstances.Contains(target))
            prefabInstances.Remove(target);

        Destroy(target);
        selector.SendMessage("Deselect", SendMessageOptions.DontRequireReceiver);

        Debug.Log("Objeto selecionado deletado.");
    }
}
