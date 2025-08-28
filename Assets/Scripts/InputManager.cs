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

    void Awake()
    {
        playerInput = new ();
        playerActions = playerInput.Player;
        motor = GetComponent<PlayerMotor>();
        look = GetComponent<PlayerLook>();

        playerActions.Jump.performed += ctx => motor.Jump();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void FixedUpdate()
    {
        // tell the playerMotor to move using the value from our movement action
        motor.ProcessMove(playerActions.Move.ReadValue<Vector2>());
    }

    void LateUpdate()
    {
        // tell the playerMotor to look using the value from our look action
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
