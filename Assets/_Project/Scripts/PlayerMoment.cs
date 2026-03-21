using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMoment : MonoBehaviour
{
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float runSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float jumpCooldown = 0.25f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private CharacterController characterController;
    private Animator animator;
    private Vector2 movementInput;
    private float verticalVelocity;
    private float currentHorizontalSpeed;
    private bool jumpPressed;
    private const float MoveThreshold = 0.1f;
    private const float SpeedDampTime = 0.1f;
    private AnimatorControllerParameterType speedParameterType = AnimatorControllerParameterType.Float;
    private bool hasSpeedParameter;
    private bool wasInAirLastFrame;
    private float airTime;
    private float nextJumpAllowedTime;
    private const float MaxAirTimeBeforeIdle = 0.5f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        DetectSpeedParameterType();
    }


    // Update is called once per frame
    void Update()
    {
        Movement();
        UpdateAnimatorParameters();
    }
    private void Movement(){
        Vector3 move = new Vector3(movementInput.x, 0f, movementInput.y);
        move = Vector3.ClampMagnitude(move, 1f);
        bool isRunning = IsRunInputHeld();
        float targetMoveSpeed = isRunning ? runSpeed : walkSpeed;

        if (move.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(move.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

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

        Vector3 velocity = move * targetMoveSpeed;
        currentHorizontalSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
        velocity.y = verticalVelocity;
        characterController.Move(velocity * Time.deltaTime);

        jumpPressed = false;
    }

    private void UpdateAnimatorParameters()
    {
        bool isMoving = currentHorizontalSpeed > MoveThreshold;
        bool isIdle = !isMoving;

        SetSpeedParameter(currentHorizontalSpeed);
        HandleJumpAnimation();
        animator.SetBool("isMove", isMoving);
        animator.SetBool("isIdle", isIdle);
    }

    private void HandleJumpAnimation()
    {
        bool isCurrentlyInAir = !characterController.isGrounded;

        // Tracking air time
        if (isCurrentlyInAir)
        {
            airTime += Time.deltaTime;
        }
        else
        {
            airTime = 0f;
        }

        // If in air too long without jumping (falling), auto-transition to Jump_Idl
        if (isCurrentlyInAir && airTime > MaxAirTimeBeforeIdle && !animator.IsInTransition(0))
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            // Only force Jump_Idle if not already in jump sequence
            if (!stateInfo.IsName("Jump_Start") && !stateInfo.IsName("Jump_Idle"))
            {
                animator.CrossFadeInFixedTime("Jump_Idle", 0.1f);
            }
        }

        // Track landing: if was in air, now grounded
        if (wasInAirLastFrame && characterController.isGrounded)
        {
            animator.SetBool("isGrounded", true);
        }
        else if (!characterController.isGrounded)
        {
            animator.SetBool("isGrounded", false);
        }

        wasInAirLastFrame = isCurrentlyInAir;
    }

    private void DetectSpeedParameterType()
    {
        hasSpeedParameter = false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == "speed")
            {
                hasSpeedParameter = true;
                speedParameterType = parameter.type;
                return;
            }
        }
    }

    private void SetSpeedParameter(float speedValue)
    {
        if (!hasSpeedParameter)
        {
            return;
        }

        if (speedParameterType == AnimatorControllerParameterType.Int)
        {
            animator.SetInteger("speed", Mathf.FloorToInt(speedValue));
            return;
        }

        animator.SetFloat("speed", speedValue, SpeedDampTime, Time.deltaTime);
    }

    private static bool IsRunInputHeld()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        return keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
    }

    private void OnMove(InputValue value)
    {
        movementInput = value.Get<Vector2>();
    }

    private void OnJump(InputValue value)
    {
        if (value.isPressed && characterController.isGrounded && Time.time >= nextJumpAllowedTime)
        {
            jumpPressed = true;
            animator.SetTrigger("JumpTrigger");
            airTime = 0f;
            nextJumpAllowedTime = Time.time + jumpCooldown;
        }
    }

}
