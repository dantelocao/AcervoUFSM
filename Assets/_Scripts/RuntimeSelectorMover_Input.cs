using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class RuntimeSelectorMover_Input : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;                         // Se vazio, usa Camera.main
    public RuntimeTransformGizmo gizmo;        // Arraste o componente do gizmo aqui

    [Header("UI (opcional)")]
    public TMP_Text selectedLabel;             // Texto para feedback

    private EditableObject _current;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        UpdateLabel();
    }

    public void ToggleGizmoMode()
    {
        if (!gizmo) return;

        gizmo.mode = gizmo.mode == RuntimeTransformGizmo.GizmoMode.Translate
            ? RuntimeTransformGizmo.GizmoMode.Rotate
            : RuntimeTransformGizmo.GizmoMode.Translate;

        UpdateLabel();
    }

    void Update()
    {
        // Deselecionar com ESC ou botão direito
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Deselect();

        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            Deselect();
    }

    /// <summary>
    /// Chame este método a partir da sua Action (ex.: tecla E).
    /// Faz raycast a partir da posição do mouse e seleciona um EditableObject.
    /// </summary>
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
                if (gizmo != null) gizmo.Attach(_current.transform);
                UpdateLabel();
                return;
            }
        }
        Deselect();
    }

    private void Deselect()
    {
        _current = null;
        if (gizmo != null) gizmo.Detach();
        UpdateLabel();
    }

    private void UpdateLabel()
    {
        if (!selectedLabel) return;

        if (_current == null)
        {
            var mode = gizmo ? gizmo.mode.ToString() : "—";
            selectedLabel.text = $"Nada selecionado\n" +
                                 $"Modo: {mode}\n[E] Selecionar\n" +
                                 $"[T] Alternar Translate/Rotate\n" +
                                 $"[Esc/Right Click] Sair modo edição";
            return;
        }

        var p = _current.transform.position;
        var r = _current.transform.eulerAngles;
        var m = gizmo ? gizmo.mode.ToString() : "—";
        selectedLabel.text =
            $"Selecionado: {_current.name}\n" +
            $"Pos: {p.x:0.##}, {p.y:0.##}, {p.z:0.##}\nRot: {r.x:0.#}, {r.y:0.#}, {r.z:0.#}\n" +
            $"Modo: {m}\nArraste nas setas para {(gizmo && gizmo.mode == RuntimeTransformGizmo.GizmoMode.Translate ? "mover" : "rotacionar")}\n" +
            $"[Esc/Right Click] Sair modo edição";

    }
}
