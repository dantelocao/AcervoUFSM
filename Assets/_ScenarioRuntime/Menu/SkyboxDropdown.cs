using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SkyboxDropdown : MonoBehaviour
{
    [Header("UI")]
    public TMP_Dropdown dropdown;                 // arraste seu TMP_Dropdown

    [Header("Skyboxes")]
    [Tooltip("Liste aqui os materiais de skybox (Cubemap, 6-sided, Procedural, etc.).")]
    public List<Material> skyboxMaterials = new List<Material>();

    [Header("Inicialização")]
    [Tooltip("Se true, tenta iniciar usando o RenderSettings.skybox atual, se ele existir na lista.")]
    public bool matchCurrentSkybox = true;
    [Tooltip("Índice default caso não haja correspondência com o skybox atual.")]
    public int defaultIndex = 0;

    void Awake()
    {
        if (!dropdown) dropdown = GetComponent<TMP_Dropdown>();
        if (!dropdown)
        {
            Debug.LogError("[SkyboxDropdown] TMP_Dropdown não atribuído.");
            enabled = false;
            return;
        }
    }

    void Start()
    {
        // Popular dropdown com os nomes dos materiais
        dropdown.ClearOptions();
        var names = new List<string>();
        foreach (var m in skyboxMaterials)
            names.Add(m ? m.name : "<vazio>");
        dropdown.AddOptions(names);

        // Descobrir índice inicial
        int idx = Mathf.Clamp(defaultIndex, 0, Mathf.Max(0, skyboxMaterials.Count - 1));
        if (matchCurrentSkybox && RenderSettings.skybox != null)
        {
            int found = skyboxMaterials.FindIndex(m => m == RenderSettings.skybox);
            if (found >= 0) idx = found;
        }

        dropdown.onValueChanged.AddListener(OnDropdownChanged);
        dropdown.SetValueWithoutNotify(idx);
        ApplySkybox(idx); // aplica ao iniciar
    }

    void OnDropdownChanged(int idx)
    {
        ApplySkybox(idx);
    }

    public void ApplySkybox(int idx)
    {
        if (idx < 0 || idx >= skyboxMaterials.Count) return;
        var mat = skyboxMaterials[idx];
        if (!mat)
        {
            Debug.LogWarning("[SkyboxDropdown] Material nulo no índice " + idx);
            return;
        }

        RenderSettings.skybox = mat;
        DynamicGI.UpdateEnvironment(); // atualiza ambient/reflections
    }

    // ==== Helpers p/ JSON ====
    // chame isso quando carregar o JSON (por nome salvo)
    public bool SetSkyboxByName(string materialName)
    {
        if (string.IsNullOrEmpty(materialName)) return false;
        int idx = skyboxMaterials.FindIndex(m => m && m.name == materialName);
        if (idx < 0) return false;

        dropdown.SetValueWithoutNotify(idx);
        ApplySkybox(idx);
        return true;
    }

    public int GetCurrentSkyboxIndex()
    {
        return Mathf.Clamp(dropdown.value, 0, Mathf.Max(0, skyboxMaterials.Count - 1));
    }

    public Material GetCurrentSkyboxMaterial()
    {
        int idx = GetCurrentSkyboxIndex();
        return (idx >= 0 && idx < skyboxMaterials.Count) ? skyboxMaterials[idx] : null;
    }
}
