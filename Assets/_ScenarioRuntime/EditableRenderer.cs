// Assets/_ScenarioRuntime/EditableRenderer.cs
using UnityEngine;

[RequireComponent(typeof(EditableObject))]
public class EditableRenderer : MonoBehaviour
{
    [Tooltip("Registry global com os materiais trocáveis (se vazio, usa MaterialRegistry.Instance)")]
    public MaterialRegistry registry;

    [Tooltip("Material inicial (id no registry). Se vazio, usa material atual do Renderer.")]
    public string startMaterialId;

    public string CurrentMaterialId { get; private set; }

    Renderer _renderer;

    MaterialRegistry GetRegistry()
        => registry != null ? registry : MaterialRegistry.Instance;

    void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>(true);
        if (_renderer == null) Debug.LogWarning($"[EditableRenderer] Sem Renderer em {name}");

        var reg = GetRegistry();

        // material inicial
        if (!string.IsNullOrEmpty(startMaterialId) && reg != null)
        {
            var mat = reg.Get(startMaterialId);
            if (mat != null)
            {
                Apply(mat, startMaterialId);
            }
        }
        else if (_renderer != null && reg != null)
        {
            // tenta deduzir o id pelo material atual
            var id = reg.GetId(_renderer.sharedMaterial);
            CurrentMaterialId = id;
        }
    }

    public void ApplyById(string materialId, bool instancePerObject = false)
    {
        var reg = GetRegistry();
        if (reg == null) return;

        var mat = reg.Get(materialId);
        if (mat != null)
        {
            Apply(mat, materialId, instancePerObject);
        }
        else
        {
            Debug.LogWarning($"[EditableRenderer] Id '{materialId}' não encontrado no registry.");
        }
    }

    public void Apply(Material mat, string materialId, bool instancePerObject = false)
    {
        if (_renderer == null || mat == null) return;
        if (instancePerObject) _renderer.material = mat;      // instância (mais memória)
        else _renderer.sharedMaterial = mat; // compartilhado (leve)
        CurrentMaterialId = materialId;
    }
}
