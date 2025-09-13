using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    // input manager
    private PlayerInputs playerInput;
    private PlayerInputs.PlayerActions playerActions;

    // player
    private PlayerMotor motor;
    private PlayerLook look;
    private RuntimeSelectorMover_Input selectorMover;

    void Awake()
    {
        playerInput = new();
        playerActions = playerInput.Player;
        motor = GetComponent<PlayerMotor>();
        look = GetComponent<PlayerLook>();
        selectorMover = GetComponent<RuntimeSelectorMover_Input>();

        playerActions.Jump.performed += ctx => motor.Jump();

        // >>> ALTERE ESTA LINHA para aplicar o bloqueio de inputs ao abrir/fechar o menu
        playerActions.Interact.performed += ctx =>
        {
            motor.Interact();                            // abre/fecha menu + cursor
            ApplyMenuState(motor.IsMenuOpen);            // ativa/desativa ações
            if (motor.IsMenuOpen) motor.StopMovement();  // opcional: para drift
        };

        playerActions.Selection.performed += ctx => selectorMover.Selection();
        playerActions.Gizmo.performed += ctx => selectorMover.ToggleGizmoMode();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    private void ApplyMenuState(bool isMenuOpen)
    {
        if (isMenuOpen)
        {
            // Desativa WASD e Look; mouse fica livre pra UI
            if (playerActions.Move.enabled) playerActions.Move.Disable();
            if (playerActions.Look.enabled) playerActions.Look.Disable();
            // Se quiser, também desative Jump/Selection/Gizmo enquanto menu aberto:
            // if (playerActions.Jump.enabled) playerActions.Jump.Disable();
            // if (playerActions.Selection.enabled) playerActions.Selection.Disable();
            // if (playerActions.Gizmo.enabled) playerActions.Gizmo.Disable();
        }
        else
        {
            // Reativa quando fechar o menu
            if (!playerActions.Move.enabled) playerActions.Move.Enable();
            if (!playerActions.Look.enabled) playerActions.Look.Enable();
            // Reative aqui se tiver desativado outros:
            // if (!playerActions.Jump.enabled) playerActions.Jump.Enable();
            // if (!playerActions.Selection.enabled) playerActions.Selection.Enable();
            // if (!playerActions.Gizmo.enabled) playerActions.Gizmo.Enable();
        }
    }

    void FixedUpdate()
    {
        // >>> NÃO processa movimento quando o menu estiver aberto
        if (!motor.IsMenuOpen)
            motor.ProcessMove(playerActions.Move.ReadValue<Vector2>());
    }

    void LateUpdate()
    {
        // >>> NÃO processa look quando o menu estiver aberto
        if (!motor.IsMenuOpen)
            look.ProcessLook(playerActions.Look.ReadValue<Vector2>());
    }

    private void OnEnable()
    {
        playerActions.Enable();
    }
    private void OnDisable()
    {
        playerActions.Disable();
    }
}
