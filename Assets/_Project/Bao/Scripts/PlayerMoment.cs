using System;
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
    [SerializeField] private float speedBlendDampTime = 0.1f;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private PlayerInput playerInput;
    private Animator animator;
    private Vector2 movementInput;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isRunning = false;
    private float lastJumpTime = -Mathf.Infinity;
    private PlayerHealth playerHealth;
    private PlayerAttack playerAttack;
    private SharedModePlayerController sharedModeController;
    private CursorLockController cursorLockController;
    enum PlayerState
    {
        Idle,
        Moving,
        Jumping,
        Falling,
        Hit,
    }
    private PlayerState currentState = PlayerState.Idle;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        //set action map to Player
        playerInput.SwitchCurrentActionMap("Player");
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        playerHealth = GetComponent<PlayerHealth>();
        playerAttack = GetComponent<PlayerAttack>();
        sharedModeController = GetComponent<SharedModePlayerController>();
        cursorLockController = GetComponent<CursorLockController>();
        if (cursorLockController == null)
        {
            cursorLockController = gameObject.AddComponent<CursorLockController>();
        }

        cursorLockController.SetActiveForLocalPlayer(true);
    }
    void Start()
    {

    }
    void Update()
    {
        if (sharedModeController != null)
        {
            return;
        }

        Movement();

        if (!isGrounded)
        {
            animator.SetBool("isGrounded", isGrounded);
            if (velocity.y > 0.1f)
                ChangeState(PlayerState.Jumping);
            else
                ChangeState(PlayerState.Falling);
        }
        else
        {
            animator.SetBool("isGrounded", isGrounded);
            if (movementInput.sqrMagnitude > 0.01f)
                ChangeState(PlayerState.Moving);
            else
                ChangeState(PlayerState.Idle);
        }
    }

    private void Movement()
    {
        if (characterController.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // Gravity
        velocity.y += gravity * Time.deltaTime;

        // Camera direction
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 move = forward * movementInput.y + right * movementInput.x;

        float maxSpeed = isRunning ? runSpeed : walkSpeed;
        float targetSpeed = movementInput.magnitude > 0.1f ? maxSpeed : 0f;
        animator.SetFloat("Speed", targetSpeed, speedBlendDampTime, Time.deltaTime);
        Vector3 finalMove = Vector3.zero;

        Vector3 aimForward = GetAimForward();
        bool aiming = IsAiming();

        if (move.magnitude > 0.1f)
        {
            // Rotate toward camera while aiming, otherwise follow move direction.
            Quaternion targetRotation = aiming && aimForward.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(aimForward)
                : Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            finalMove = move.normalized * maxSpeed;
        }
        else if (aiming && aimForward.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(aimForward);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        finalMove.y = velocity.y;

        characterController.Move(finalMove * Time.deltaTime);
        isGrounded = characterController.isGrounded;
    }
    public void ResetTrigger(string triggerName)
    {
        animator.ResetTrigger(triggerName);
    }
    private void ChangeState(PlayerState newState)
    {
        if (currentState == newState) return;

        switch (currentState)
        {
            case PlayerState.Idle:
                animator.SetBool("isIdle", false);
                break;
            case PlayerState.Moving:
                animator.SetBool("isMove", false);
                break;
            case PlayerState.Falling:
                animator.SetBool("isFalling", false);
                break;
            case PlayerState.Hit:
                animator.SetTrigger("GetHit");
                break;

        }

        currentState = newState;

        switch (newState)
        {
            case PlayerState.Idle:
                animator.SetBool("isIdle", true);
                break;
            case PlayerState.Moving:
                animator.SetBool("isMove", true);
                break;
            case PlayerState.Jumping:
                animator.SetTrigger("JumpTrigger");
                break;
            case PlayerState.Falling:
                animator.SetBool("isFalling", true);
                break;
        }

    }


    public void OnMove(InputAction.CallbackContext context)
    {
        if (sharedModeController != null) return;
        if (playerHealth != null && playerHealth.IsDead) return;
        movementInput = context.ReadValue<Vector2>();
    }
    public void OnSprint(InputAction.CallbackContext context)
    {
        if (sharedModeController != null) return;
        if (playerHealth != null && playerHealth.IsDead) return;
        if (context.performed)
        {
            isRunning = true;

        }
        else if (context.canceled)
        {
            isRunning = false;
        }
    }



    public void OnJump(InputAction.CallbackContext context)
    {
        if (sharedModeController != null) return;
        if (playerHealth != null && playerHealth.IsDead) return;
        if (!context.performed) return;

        bool canJump = characterController.isGrounded &&
        (Time.time - lastJumpTime) >= jumpCooldown;

        if (canJump)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            lastJumpTime = Time.time;
        }
    }

    private bool IsAiming()
    {
        return playerAttack != null && playerAttack.IsAiming;
    }

    private Vector3 GetAimForward()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            return transform.forward;
        }

        // Get aim direction from screen center, not camera.forward
        Ray screenCenterRay = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 forward = screenCenterRay.direction;
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.0001f)
        {
            return transform.forward;
        }

        return forward.normalized;
    }
}
