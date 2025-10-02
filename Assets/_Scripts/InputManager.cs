using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    // input manager
    private PlayerInputs playerInput;                    // use aqui o NOME da classe gerada pelo seu .inputactions
    private PlayerInputs.PlayerActions playerActions;

    // player
    private PlayerMotor motor;
    private PlayerLook look;
    private RuntimeSelectorMover_Input selectorMover;

    void Awake()
    {
        playerInput = new PlayerInputs();                // certifique-se de que o nome bate com a classe gerada

        playerActions = playerInput.Player;
        motor = GetComponent<PlayerMotor>();
        look = GetComponent<PlayerLook>();
        selectorMover = GetComponent<RuntimeSelectorMover_Input>();

        playerActions.Jump.performed += ctx => motor.Jump();

        // Abrir/fechar DEBUG MENU (o seu Interact atual)
        playerActions.Interact.performed += ctx =>
        {
            motor.Interact();                           // toggle debugMenu
            ApplyMenuState(motor.IsMenuOpen);
            if (motor.IsMenuOpen) motor.StopMovement();
        };

        // >>> NOVO: Abrir/fechar o MENU da ESQUERDA com a Action "Menu" (R / Button North)
        playerActions.Menu.performed += ctx =>
        {
            motor.ToggleLeftMenu();                     // toggle painel da esquerda
            ApplyMenuState(motor.IsMenuOpen);
            if (motor.IsMenuOpen) motor.StopMovement();
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
            // Desativa movimentação e look; mantém Menu/Interact ativos p/ conseguir FECHAR
            if (playerActions.Move.enabled) playerActions.Move.Disable();
            if (playerActions.Look.enabled) playerActions.Look.Disable();
            // (Opcional) desative também Jump/Selection/Gizmo se preferir:
            // if (playerActions.Jump.enabled)      playerActions.Jump.Disable();
            // if (playerActions.Selection.enabled) playerActions.Selection.Disable();
            // if (playerActions.Gizmo.enabled)     playerActions.Gizmo.Disable();
        }
        else
        {
            if (!playerActions.Move.enabled) playerActions.Move.Enable();
            if (!playerActions.Look.enabled) playerActions.Look.Enable();
            // Reative os que tiver desativado:
            // if (!playerActions.Jump.enabled)      playerActions.Jump.Enable();
            // if (!playerActions.Selection.enabled) playerActions.Selection.Enable();
            // if (!playerActions.Gizmo.enabled)     playerActions.Gizmo.Enable();
        }
    }

    void FixedUpdate()
    {
        if (!motor.IsMenuOpen)
            motor.ProcessMove(playerActions.Move.ReadValue<Vector2>());
    }

    void LateUpdate()
    {
        if (!motor.IsMenuOpen)
            look.ProcessLook(playerActions.Look.ReadValue<Vector2>());
    }

    private void OnEnable() => playerActions.Enable();
    private void OnDisable() => playerActions.Disable();
}
