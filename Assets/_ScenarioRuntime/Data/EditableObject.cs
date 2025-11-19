using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class EditableObject : MonoBehaviour
{
    [SerializeField] private string id;          // ID estável
    [SerializeField] private string prefabPath;  // ID textual do PrefabRegistry

    public string Id => id;
    public string PrefabPath => prefabPath;

    // Só o ScenarioIO deve alterar prefabPath.
    public void SetPrefabPath(string path)
    {
        prefabPath = path;
    }

    private void Reset()
    {
        GenerateIdIfNeeded();
    }

    private void OnValidate()
    {
        GenerateIdIfNeeded();
    }

    private void GenerateIdIfNeeded()
    {
        // Se estiver editando o prefab original  NÃO salva ID nele
#if UNITY_EDITOR
        if (!EditorApplication.isPlaying)
        {
            if (PrefabUtility.IsPartOfPrefabAsset(gameObject))
            {
                id = "";              // IMPORTANTE: remove ID do prefab
                prefabPath = "";      // prefabs não definem o próprio path
                return;
            }
        }
#endif

        // No Runtime  garantir ID
        if (string.IsNullOrEmpty(id))
        {
            id = System.Guid.NewGuid().ToString("N");
        }
    }
}
