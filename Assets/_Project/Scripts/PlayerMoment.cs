using Fusion;
using UnityEngine;

public class PlayerMoment : NetworkBehaviour
{
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float runSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float jumpCooldown = 0.25f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private CharacterController characterController;
    private Animator animator;
    private float verticalVelocity;
    private float currentHorizontalSpeed;
    private const float MoveThreshold = 0.1f;
    private const float SpeedDampTime = 0.1f;
    private AnimatorControllerParameterType speedParameterType = AnimatorControllerParameterType.Float;
    private bool hasSpeedParameter;
    private bool wasInAirLastFrame;
    private float airTime;
    private const float MaxAirTimeBeforeIdle = 0.5f;
    [Networked] private NetworkButtons PreviousButtons { get; set; }
    [Networked] private TickTimer JumpCooldownTimer { get; set; }
    [Networked] private Vector3 NetworkedPosition { get; set; }
    [Networked] private Quaternion NetworkedRotation { get; set; }

    private bool _characterControllerInitialized;

    public override void Spawned()
    {
        EnsureReferences();

        if (HasStateAuthority)
        {
            NetworkedPosition = transform.position;
            NetworkedRotation = transform.rotation;
            characterController.enabled = true;
            _characterControllerInitialized = true;
            return;
        }

        characterController.enabled = false;
        _characterControllerInitialized = true;
    }

    public override void FixedUpdateNetwork()
    {
        EnsureReferences();

        if (!_characterControllerInitialized)
        {
            characterController.enabled = HasStateAuthority;
            _characterControllerInitialized = true;
        }

        if (!HasStateAuthority)
        {
            return;
        }

        PlayerNetworkInputData inputData = default;
        GetInput(out inputData);

        Movement(inputData);
        NetworkedPosition = transform.position;
        NetworkedRotation = transform.rotation;
        UpdateAnimatorParameters();
    }

    public override void Render()
    {
        if (HasStateAuthority)
        {
            return;
        }

        transform.SetPositionAndRotation(NetworkedPosition, NetworkedRotation);
    }

    private void EnsureReferences()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (animator != null && !hasSpeedParameter)
        {
            DetectSpeedParameterType();
        }
    }

    private void Movement(PlayerNetworkInputData inputData)
    {
        Vector3 move = new Vector3(inputData.Move.x, 0f, inputData.Move.y);
        move = Vector3.ClampMagnitude(move, 1f);

        NetworkButtons pressed = inputData.Buttons.GetPressed(PreviousButtons);
        bool jumpPressed = pressed.IsSet((int)PlayerInputButton.Jump);
        bool isRunning = inputData.Buttons.IsSet((int)PlayerInputButton.Run);
        PreviousButtons = inputData.Buttons;

        float targetMoveSpeed = isRunning ? runSpeed : walkSpeed;

        if (move.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(move.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Runner.DeltaTime);
        }

        if (characterController.isGrounded)
        {
            if (verticalVelocity < 0f)
            {
                // Keep the controller grounded instead of accumulating downward speed.
                verticalVelocity = -2f;
            }

            if (jumpPressed && JumpCooldownTimer.ExpiredOrNotRunning(Runner))
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                animator.SetTrigger("JumpTrigger");
                airTime = 0f;
                JumpCooldownTimer = TickTimer.CreateFromSeconds(Runner, jumpCooldown);
            }
        }

        verticalVelocity += gravity * Runner.DeltaTime;

        Vector3 velocity = move * targetMoveSpeed;
        currentHorizontalSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
        velocity.y = verticalVelocity;
        characterController.Move(velocity * Runner.DeltaTime);
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
            airTime += Runner.DeltaTime;
        }
        else
        {
            airTime = 0f;
        }

        // If in air too long without jumpng (falling), auto-transition to Jump_Idl
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

        animator.SetFloat("speed", speedValue, SpeedDampTime, Runner.DeltaTime);
    }

}
