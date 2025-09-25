using TMPro;
using UnityEngine;

public class MaterialSelectionController : MonoBehaviour
{
    [Header("Refs")]
    public MaterialRegistry registry;      // deixe vazio para usar Instance
    public TMP_Dropdown dropdown;          // seu dropdown
    public RuntimeSelectorMover_Input selector; // seu seletor de objetos

    EditableRenderer _current;

    void Start()
    {
        if (registry == null) registry = MaterialRegistry.Instance;
        if (registry == null) { Debug.LogError("[MSC] Registry não encontrado."); enabled = false; return; }
        if (dropdown == null) { Debug.LogError("[MSC] Dropdown não atribuído."); enabled = false; return; }

        // popular IDs
        dropdown.ClearOptions();
        dropdown.AddOptions(new System.Collections.Generic.List<string>(registry.AllIds()));
        dropdown.onValueChanged.AddListener(OnDropdownChanged);
        dropdown.interactable = false;
    }

    // Chame isto quando o alvo mudar
    public void SetTarget(EditableObject eo)
    {
        _current = eo ? eo.GetComponent<EditableRenderer>() : null;
        dropdown.interactable = _current != null;

        if (_current != null)
        {
            var id = _current.CurrentMaterialId;
            var idx = Mathf.Max(0, dropdown.options.FindIndex(o => o.text == id));
            dropdown.SetValueWithoutNotify(idx);
        }
    }

    void OnDropdownChanged(int idx)
    {
        if (_current == null) return;
        var id = dropdown.options[idx].text;
        _current.ApplyById(id, instancePerObject: false); // sharedMaterial: melhor p/ WebGL
    }
}
