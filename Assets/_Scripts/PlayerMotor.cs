using UnityEngine;

public class PlayerMotor : MonoBehaviour
{
    // controller
    private CharacterController characterController;

    [Header("Menus")]
    public GameObject debugMenu;
    private bool debugMenuActive = false;

    [Tooltip("Painel da esquerda (MenuPanel).")]
    public GameObject leftMenuPanel;
    private bool leftMenuActive = false;

    // variables
    private bool isGrounded;
    public Vector3 playerVelocity;
    public float speed = 5f;
    public float gravity = -9.8f;
    public float jumpHeight = 3f;

    // fica true se QUALQUER menu estiver aberto
    public bool IsMenuOpen => debugMenuActive || leftMenuActive;

    public void StopMovement() => playerVelocity = Vector3.zero;

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        if (debugMenu) debugMenu.SetActive(false);
        if (leftMenuPanel) leftMenuPanel.SetActive(false);

        TravarCursor();
    }

    void Update()
    {
        isGrounded = characterController.isGrounded;
    }

    public void ProcessMove(Vector2 input)
    {
        if (IsMenuOpen) return;

        Vector3 moveDirection = Vector3.zero;
        moveDirection.x = input.x;
        moveDirection.z = input.y;

        characterController.Move(speed * Time.deltaTime * transform.TransformDirection(moveDirection));

        playerVelocity.y += gravity * Time.deltaTime;
        if (isGrounded && playerVelocity.y < 0) playerVelocity.y = -2f;

        characterController.Move(playerVelocity * Time.deltaTime);
    }

    public void Jump()
    {
        if (IsMenuOpen) return;

        if (isGrounded)
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -3.0f * gravity);
    }

    // === Toggle do DEBUG MENU (já existia) ===
    public void Interact()
    {
        if (!debugMenu) return;

        debugMenuActive = !debugMenuActive;
        debugMenu.SetActive(debugMenuActive);

        if (debugMenuActive) LiberarCursor();
        else if (!IsMenuOpen) TravarCursor();
    }

    // === NOVO: Toggle do Menu da ESQUERDA (usado pela Action "Menu" / tecla R) ===
    public void ToggleLeftMenu()
    {
        if (!leftMenuPanel) return;

        leftMenuActive = !leftMenuActive;
        leftMenuPanel.SetActive(leftMenuActive);

        if (leftMenuActive) LiberarCursor();
        else if (!IsMenuOpen) TravarCursor();
    }

    void LiberarCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void TravarCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
