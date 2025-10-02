using UnityEngine;

/// <summary>
/// Marca um objeto da cena como "slot" que recebe a textura da obra.
/// Aplique este componente no GameObject do quadro (ou em um filho) e
/// aponte o Renderer que deve receber a textura (geralmente o MeshRenderer do quad).
/// </summary>
[DisallowMultipleComponent]
public class ArtworkSlot : MonoBehaviour
{
    [Tooltip("Renderer que receberá a textura da obra (ex.: MeshRenderer do quad).")]
    public Renderer targetRenderer;

    [Header("Auto-configuração")]
    [Tooltip("Se marcado, tenta localizar automaticamente um Renderer neste objeto/filhos quando vazio.")]
    public bool autoFindWhenEmpty = true;

    /// <summary>
    /// Renderer válido para aplicação (null se não encontrado).
    /// </summary>
    public Renderer TargetRenderer => targetRenderer;

    /// <summary>
    /// Retorna true se este slot está pronto para receber textura.
    /// </summary>
    public bool IsValid => targetRenderer != null;

    // Chamado ao adicionar o componente
    void Reset()
    {
        AutoFindRendererIfNeeded(force: true);
    }

    // Chamado ao alterar algo no Inspector
    void OnValidate()
    {
        AutoFindRendererIfNeeded(force: false);
    }

    private void AutoFindRendererIfNeeded(bool force)
    {
        if (!autoFindWhenEmpty && !force) return;

        if (targetRenderer == null || force)
        {
            // 1) Tenta no próprio GO
            if (!TryGetComponent(out targetRenderer))
            {
                // 2) Tenta em filhos (caso o quad esteja em um filho)
                targetRenderer = GetComponentInChildren<Renderer>(true);
            }
        }
    }
}
