// Assets/_ScenarioRuntime/MaterialDropdownForSelector.cs
using TMPro;
using UnityEngine;

public class MaterialDropdownForSelector : MonoBehaviour
{
    [Header("Refs")]
    public RuntimeSelectorMover_Input selector;   // arraste seu selector
    public TMP_Dropdown dropdown;                 // arraste o TMP_Dropdown
    public MaterialRegistry registry;             // deixe vazio para usar Instance

    [Header("UI Visibility")]
    [Tooltip("Deixe vazio para usar o próprio dropdown. Pode ser um pai com label + dropdown.")]
    public GameObject container;

    EditableObject _lastEO;
    EditableRenderer _currentER;

    void Awake()
    {
        if (!container && dropdown) container = dropdown.gameObject;
    }

    void OnEnable()
    {
        if (selector != null)
            selector.OnSelectionChanged += HandleSelectionChanged;
    }

    void OnDisable()
    {
        if (selector != null)
            selector.OnSelectionChanged -= HandleSelectionChanged;
    }

    void Start()
    {
        if (registry == null) registry = MaterialRegistry.Instance;
        if (registry == null) { Debug.LogError("[MatDD] MaterialRegistry não encontrado."); enabled = false; return; }

        if (!dropdown) { Debug.LogError("[MatDD] TMP_Dropdown não atribuído."); enabled = false; return; }

        // popular IDs do catálogo
        dropdown.ClearOptions();
        var ids = new System.Collections.Generic.List<string>(registry.AllIds());
        dropdown.AddOptions(ids);

        dropdown.onValueChanged.AddListener(OnDropdownChanged);
        dropdown.interactable = false;

        // começa oculto, já que não há seleção ao iniciar
        if (container) container.SetActive(false);
    }

    void HandleSelectionChanged(EditableObject eo)
    {
        _lastEO = eo;
        _currentER = null;

        // Mostra/oculta o container pelo fato de haver (ou não) algo selecionado
        if (container) container.SetActive(eo != null);

        if (eo != null)
        {
            _currentER = eo.GetComponent<EditableRenderer>();
            dropdown.interactable = _currentER != null;

            if (_currentER != null)
            {
                var id = _currentER.CurrentMaterialId;
                var idx = Mathf.Max(0, dropdown.options.FindIndex(o => o.text == id));
                dropdown.SetValueWithoutNotify(idx);
            }
            else
            {
                // Sem EditableRenderer, mantém opções mas não interage
                dropdown.SetValueWithoutNotify(0);
            }
        }
        else
        {
            dropdown.interactable = false;
        }
    }

    void OnDropdownChanged(int idx)
    {
        if (_currentER == null) return;
        var id = dropdown.options[idx].text;
        _currentER.ApplyById(id, instancePerObject: false); // sharedMaterial (bom p/ WebGL)
    }
}
