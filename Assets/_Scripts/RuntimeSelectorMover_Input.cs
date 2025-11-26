using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class RuntimeSelectorMover_Input : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;
    public RuntimeTransformGizmo gizmo;

    [Header("UI (informativo)")]
    public GameObject infoPanel;
    public TMP_Text selectedLabel;

    [Header("UI (edição por números)")]
    public GameObject inputPanel;
    // Translate
    public TMP_InputField posXField;
    public TMP_InputField posYField;
    public TMP_InputField posZField;
    // Rotation (graus)
    public TMP_InputField rotXField;
    public TMP_InputField rotYField;
    public TMP_InputField rotZField;
    // Scale (local)
    public TMP_InputField scaleXField;
    public TMP_InputField scaleYField;
    public TMP_InputField scaleZField;

    private EditableObject _current;

    // === NOVO: expõe o alvo atual ===
    public EditableObject Current => _current;

    // === NOVO: evento disparado quando a seleção muda ===
    public event System.Action<EditableObject> OnSelectionChanged;

    private bool _uiEditMode = false;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        SetEditMode(false, updateFields: false);
        UpdateLabel();
    }

    public void ToggleGizmoMode()
    {
        if (!gizmo) return;

        switch (gizmo.mode)
        {
            case RuntimeTransformGizmo.GizmoMode.Translate:
                gizmo.mode = RuntimeTransformGizmo.GizmoMode.Rotate;
                break;
            case RuntimeTransformGizmo.GizmoMode.Rotate:
                gizmo.mode = RuntimeTransformGizmo.GizmoMode.Scale;
                break;
            default:
                gizmo.mode = RuntimeTransformGizmo.GizmoMode.Translate;
                break;
        }
        UpdateLabel();
    }

    void Update()
    {
        if (_current != null && Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            SetEditMode(!_uiEditMode, updateFields: true);
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (_uiEditMode) SetEditMode(false, updateFields: false);
            else Deselect();
        }

        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (_uiEditMode) SetEditMode(false, updateFields: false);
            else Deselect();
        }
    }

    public void Selection()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) { Debug.LogWarning("[Selector] Não há Camera definida."); return; }
        if (Mouse.current == null) { Debug.LogWarning("[Selector] InputSystem sem Mouse."); return; }

        var ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out var hit, 2000f))
        {
            var eo = hit.collider.GetComponentInParent<EditableObject>();
            if (eo != null)
            {
                _current = eo;

                // === NOVO: notifica ouvintes (ex.: dropdown) ===
                OnSelectionChanged?.Invoke(_current);

                if (gizmo != null) gizmo.Attach(_current.transform);
                SetEditMode(false, updateFields: false);
                UpdateLabel();
                return;
            }
        }
        Deselect();
    }

    private void Deselect()
    {
        _current = null;

        // === NOVO: notifica que não há seleção ===
        OnSelectionChanged?.Invoke(null);

        if (gizmo != null) gizmo.Detach();
        SetEditMode(false, updateFields: false);
        UpdateLabel();
    }

    private void SetEditMode(bool enable, bool updateFields)
    {
        _uiEditMode = enable;

        if (infoPanel) infoPanel.SetActive(true);
        if (inputPanel) inputPanel.SetActive(enable);

        if (gizmo != null) gizmo.ignoreUI = enable;

        Cursor.lockState = enable ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = enable;

        if (enable && updateFields && _current != null)
        {
            FillFieldsFromCurrent();
        }

        UpdateLabel();
    }

    private void FillFieldsFromCurrent()
    {
        if (_current == null) return;
        var t = _current.transform;

        var p = t.position;
        if (posXField) posXField.text = p.x.ToString("0.###");
        if (posYField) posYField.text = p.y.ToString("0.###");
        if (posZField) posZField.text = p.z.ToString("0.###");

        var e = t.rotation.eulerAngles; // world degrees
        if (rotXField) rotXField.text = e.x.ToString("0.###");
        if (rotYField) rotYField.text = e.y.ToString("0.###");
        if (rotZField) rotZField.text = e.z.ToString("0.###");

        var s = t.localScale;
        if (scaleXField) scaleXField.text = s.x.ToString("0.###");
        if (scaleYField) scaleYField.text = s.y.ToString("0.###");
        if (scaleZField) scaleZField.text = s.z.ToString("0.###");

        // foco inicial
        if (posXField) posXField.Select();
    }

    /// <summary>
    /// Aplica os 9 valores nos componentes do Transform atual.
    /// (Ligue este método ao botão "Aplicar" ou a um atalho)
    /// </summary>
    public void ApplyInputs()
    {
        if (_current == null) return;
        var t = _current.transform;

        // Parse posição
        if (!TryParseLocaleFloat(posXField?.text, out float px) ||
            !TryParseLocaleFloat(posYField?.text, out float py) ||
            !TryParseLocaleFloat(posZField?.text, out float pz))
        {
            Debug.LogWarning("[Selector] Posição inválida.");
            return;
        }

        // Parse rotação (graus)
        if (!TryParseLocaleFloat(rotXField?.text, out float rx) ||
            !TryParseLocaleFloat(rotYField?.text, out float ry) ||
            !TryParseLocaleFloat(rotZField?.text, out float rz))
        {
            Debug.LogWarning("[Selector] Rotação inválida.");
            return;
        }

        // Parse escala
        if (!TryParseLocaleFloat(scaleXField?.text, out float sx) ||
            !TryParseLocaleFloat(scaleYField?.text, out float sy) ||
            !TryParseLocaleFloat(scaleZField?.text, out float sz))
        {
            Debug.LogWarning("[Selector] Escala inválida.");
            return;
        }

        // Aplicar
        t.position = new Vector3(px, py, pz);

        // Normaliza rotação para [-180,180] antes de gerar o Quaternion (opcional)
        rx = NormalizeAngle(rx);
        ry = NormalizeAngle(ry);
        rz = NormalizeAngle(rz);
        t.rotation = Quaternion.Euler(rx, ry, rz);

        // Clamp de escala para evitar zero/negativo
        const float MinScale = 0.001f;
        sx = Mathf.Max(MinScale, sx);
        sy = Mathf.Max(MinScale, sy);
        sz = Mathf.Max(MinScale, sz);
        t.localScale = new Vector3(sx, sy, sz);

        UpdateLabel();
    }

    private static float NormalizeAngle(float aDeg)
    {
        aDeg %= 360f;
        if (aDeg > 180f) aDeg -= 360f;
        if (aDeg < -180f) aDeg += 360f;
        return aDeg;
    }

    private static bool TryParseLocaleFloat(string raw, out float value)
    {
        value = 0f;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        raw = raw.Trim();

        if (float.TryParse(raw, System.Globalization.NumberStyles.Float,
                           System.Globalization.CultureInfo.CurrentCulture, out value))
            return true;

        char dec = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];
        char other = dec == '.' ? ',' : '.';
        if (raw.IndexOf(other) >= 0)
        {
            var swapped = raw.Replace(other, dec);
            return float.TryParse(swapped, System.Globalization.NumberStyles.Float,
                                  System.Globalization.CultureInfo.CurrentCulture, out value);
        }
        return false;
    }

    private void UpdateLabel()
    {
        if (!selectedLabel) return;

        var modeStr = gizmo ? gizmo.mode.ToString() : "—";

        if (_current == null)
        {
            selectedLabel.text =
                $"Nada selecionado\n" +
                $"Modo: {modeStr}\n[E] Selecionar\n" +
                $"[T] Alternar Translate/Rotate/Scale\n" +
                $"[Esc/Right Click] Sair modo edição";
            return;
        }

        var t = _current.transform;
        var p = t.position;
        var r = t.eulerAngles;
        var s = t.localScale;

        string action = "editar";
        if (gizmo)
        {
            switch (gizmo.mode)
            {
                case RuntimeTransformGizmo.GizmoMode.Translate: action = "mover"; break;
                case RuntimeTransformGizmo.GizmoMode.Rotate: action = "rotacionar"; break;
                case RuntimeTransformGizmo.GizmoMode.Scale: action = "escalar"; break;
            }
        }

        string hint = _uiEditMode ? "[Enter/Aplicar] para confirmar • [Tab] voltar ao jogo"
                                  : "[Tab] Editar por números";

        selectedLabel.text =
            $"Selecionado: {_current.name}\n" +
            $"Pos: {p.x:0.##}, {p.y:0.##}, {p.z:0.##}\n" +
            $"Rot: {r.x:0.#}, {r.y:0.#}, {r.z:0.#}\n" +
            $"Scale: {s.x:0.##}, {s.y:0.##}, {s.z:0.##}\n" +
            $"Modo: {modeStr}\n" +
            $"Arraste nas setas para {action}\n" +
            $"{hint}\n" +
            $"[Esc/Right Click] Sair modo edição";
    }
}
