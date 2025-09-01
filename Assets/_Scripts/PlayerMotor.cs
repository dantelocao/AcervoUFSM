using UnityEngine;

public class PlayerMotor : MonoBehaviour
{
    // controller
    private CharacterController characterController;

    public GameObject debugMenu;
    private bool debugMenuActive = false;

    // variables
    private bool isGrounded;
    public Vector3 playerVelocity; // velocity applied in Y axis for gravity
    public float speed = 5f;
    public float gravity = -9.8f;
    public float jumpHeight = 3f;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        isGrounded = characterController.isGrounded;
    }

    public void ProcessMove(Vector2 input)
    {
        Vector3 moveDirection = Vector3.zero;
        moveDirection.x = input.x;
        moveDirection.z = input.y;
        characterController.Move(speed * Time.deltaTime * transform.TransformDirection(moveDirection));
        playerVelocity.y += gravity * Time.deltaTime;
        if (isGrounded && playerVelocity.y < 0)
            playerVelocity.y = -2f;
        characterController.Move(playerVelocity * Time.deltaTime);
    }

    public void Jump()
    {
        print("jump");
        if (isGrounded)
        {
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -3.0f * gravity);
        }
    }

    public void Interact()
    {
        if (debugMenu)
        {
            debugMenuActive = !debugMenuActive;
            debugMenu.SetActive(debugMenuActive);
            if (debugMenuActive)
                LiberarCursor();
            else
                TravarCursor();
        }

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
