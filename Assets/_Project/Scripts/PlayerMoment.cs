using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMoment : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private CharacterController characterController;
    private Vector2 movementInput;
    private float verticalVelocity;
    private bool jumpPressed;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        characterController = GetComponent<CharacterController>();

    }


    // Update is called once per frame
    void Update()
    {
        MoveMent();
    }
    private void MoveMent(){
        Vector3 move = new Vector3(movementInput.x, 0f, movementInput.y);
        move = transform.TransformDirection(move);

        if (characterController.isGrounded)
        {
            if (verticalVelocity < 0f)
            {
                // Keep the controller grounded instead of accumulating downward speed.
                verticalVelocity = -2f;
            }

            if (jumpPressed)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = move * speed;
        velocity.y = verticalVelocity;
        characterController.Move(velocity * Time.deltaTime);

        jumpPressed = false;
    }

    private void OnMove(InputValue value)
    {
        movementInput = value.Get<Vector2>();
    }

    private void OnJump(InputValue value)
    {
        if (value.isPressed)
        {
            jumpPressed = true;
        }
    }

}
