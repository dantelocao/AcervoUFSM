// Assets/_ScenarioRuntime/EditableObject.cs
using UnityEngine;

[DisallowMultipleComponent]
public class EditableObject : MonoBehaviour
{
    [SerializeField] private string id;

    /// <summary>ID estável do objeto (use no JSON).</summary>
    public string Id => id;

    private void Reset()
    {
#if UNITY_EDITOR
        // Gera um GUID quando o componente é adicionado.
        if (string.IsNullOrEmpty(id))
            id = System.Guid.NewGuid().ToString("N");
#endif
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(id))
            id = System.Guid.NewGuid().ToString("N");
#endif
    }
}
