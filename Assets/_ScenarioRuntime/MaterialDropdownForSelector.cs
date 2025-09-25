// Assets/_ScenarioRuntime/MaterialDropdownForSelector.cs
using TMPro;
using UnityEngine;

public class MaterialDropdownForSelector : MonoBehaviour
{
    [Header("Refs")]
    public RuntimeSelectorMover_Input selector;   // arraste seu selector
    public TMP_Dropdown dropdown;                 // arraste o TMP_Dropdown
    public MaterialRegistry registry;             // deixe vazio para usar Instance

    EditableObject _lastEO;
    EditableRenderer _currentER;

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
    }

    void Update()
    {
        if (!selector) return;

        // pega o EO atual via reflection pública (abaixo tem um getter se você quiser adicionar)
        var eo = GetCurrentEditableObject(selector);

        if (eo != _lastEO)
        {
            _lastEO = eo;
            _currentER = null;

            if (eo != null)
            {
                _currentER = eo.GetComponent<EditableRenderer>();
                dropdown.interactable = _currentER != null;

                // posiciona o dropdown no material atual do alvo
                if (_currentER != null)
                {
                    var id = _currentER.CurrentMaterialId;
                    var idx = Mathf.Max(0, dropdown.options.FindIndex(o => o.text == id));
                    dropdown.SetValueWithoutNotify(idx);
                }
            }
            else
            {
                dropdown.interactable = false;
            }
        }
    }

    void OnDropdownChanged(int idx)
    {
        if (_currentER == null) return;
        var id = dropdown.options[idx].text;
        _currentER.ApplyById(id, instancePerObject: false); // sharedMaterial (melhor p/ WebGL)
    }

    // === util ===
    EditableObject GetCurrentEditableObject(RuntimeSelectorMover_Input sel)
    {
        // Se você acrescentar uma propriedade pública Current no seu selector, troque por: return sel.Current;
        // Como alternativa simples sem mexer no seu arquivo, você pode expor _current como internal e usar InternalsVisibleTo,
        // mas o mais prático é criar um accessor público (ver Opção B).
        // Aqui vou tentar pegar via método privado? Não dá. Então sugiro a pequena mudança da Opção B abaixo.
        return _cachedGetter?.Invoke(sel);
    }

    // truque pequeno: cachear um delegate para ler via reflexão uma prop pública "Current" se existir
    static System.Func<RuntimeSelectorMover_Input, EditableObject> _cachedGetter =
        CreateGetter();

    static System.Func<RuntimeSelectorMover_Input, EditableObject> CreateGetter()
    {
        var t = typeof(RuntimeSelectorMover_Input);
        // tenta achar uma propriedade pública chamada "Current"
        var prop = t.GetProperty("Current", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (prop != null && prop.PropertyType == typeof(EditableObject))
        {
            return (RuntimeSelectorMover_Input s) => (EditableObject)prop.GetValue(s);
        }
        // se não existir, retorna função que sempre devolve null (a Opção B resolve isso)
        return (RuntimeSelectorMover_Input s) => null;
    }
}
