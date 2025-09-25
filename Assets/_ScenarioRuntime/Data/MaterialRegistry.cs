// Assets/_ScenarioRuntime/MaterialRegistry.cs
using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)] // inicializa cedo
public class MaterialRegistry : MonoBehaviour
{
    public static MaterialRegistry Instance { get; private set; }

    [Header("Fonte de dados (ScriptableObject)")]
    [SerializeField] private MaterialRegistryData data;

    // Mapas internos
    private Dictionary<string, Material> _byId;
    private Dictionary<Material, string> _byMat;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[MaterialRegistry] Já existe uma instância. Destruindo a duplicata.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        BuildIndexes();
        // Opcional: manter entre cenas
        // DontDestroyOnLoad(gameObject);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Atualiza índices no editor quando mudar o asset
        if (Application.isPlaying) return;
        BuildIndexes();
    }
#endif

    private void BuildIndexes()
    {
        _byId = new Dictionary<string, Material>(StringComparer.Ordinal);
        _byMat = new Dictionary<Material, string>();

        if (data == null)
        {
            Debug.LogWarning("[MaterialRegistry] Data (ScriptableObject) não atribuído.");
            return;
        }

        foreach (var e in data.entries)
        {
            if (e == null) continue;
            if (string.IsNullOrWhiteSpace(e.id)) continue;
            if (e.material == null) continue;

            _byId[e.id] = e.material;
            if (!_byMat.ContainsKey(e.material))
                _byMat[e.material] = e.id;
        }
    }

    // === API esperada pelo seu SceneStateManager ===

    /// <summary>Retorna o Material pelo id (ou null se não existir).</summary>
    public Material Get(string id)
    {
        if (string.IsNullOrEmpty(id) || _byId == null) return null;
        return _byId.TryGetValue(id, out var mat) ? mat : null;
    }

    /// <summary>Retorna o id correspondente a um Material (ou null).</summary>
    public string GetId(Material material)
    {
        if (material == null || _byMat == null) return null;
        return _byMat.TryGetValue(material, out var id) ? id : null;
    }

    // (Opcional) útil para UI/listas
    public IEnumerable<string> AllIds()
    {
        if (data == null) yield break;
        foreach (var e in data.entries)
            if (e != null && !string.IsNullOrWhiteSpace(e.id))
                yield return e.id;
    }
}
