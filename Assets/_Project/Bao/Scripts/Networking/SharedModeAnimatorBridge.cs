using UnityEngine;

[DisallowMultipleComponent]
public class SharedModeAnimatorBridge : MonoBehaviour
{
    [SerializeField] private SharedModePlayerController controller;
    [SerializeField] private Animator animator;
    [SerializeField] private Fusion.NetworkCharacterController networkCharacterController;
    [SerializeField] private BowHandVisualAnimationEvents bowHandVisualEvents;

    private int lastHitSequence = -1;
    private int lastAttackSequence = -1;
    private bool deadTriggered;
    private bool wasBlocking;
    private bool wasJumping;
    private bool wasAiming;

    private const string AttackSwordTrigger = "AttackSword";
    private const string AttackBowTrigger = "AttackBow";
    private const string StartAimTrigger = "startAim";

    private void Awake()
    {
        if (controller == null)
        {
            controller = GetComponent<SharedModePlayerController>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (networkCharacterController == null)
        {
            networkCharacterController = GetComponent<Fusion.NetworkCharacterController>();
        }

        if (bowHandVisualEvents == null)
        {
            bowHandVisualEvents = GetComponentInChildren<BowHandVisualAnimationEvents>(true);
        }
    }

    private void Update()
    {
        if (controller == null || animator == null)
        {
            return;
        }

        bool isDead = controller.IsDead;
        SetBoolIfExists("isDead", isDead);

        if (isDead)
        {
            if (wasAiming)
            {
                bowHandVisualEvents?.AnimationEvent_DeactivateBowInHand();
            }

            if (!deadTriggered)
            {
                SetTriggerIfExists("DeadTrigger");
                deadTriggered = true;
            }

            SetBoolIfExists("isMove", false);
            SetBoolIfExists("isIdle", true);
            SetBoolIfExists("isFalling", false);
            SetBoolIfExists("isBlocking", false);
            SetBoolIfExists("isAiming", false);
            animator.SetFloat("Speed", 0f, 0.08f, Time.deltaTime);
            wasJumping = false;
            wasAiming = false;
            return;
        }

        deadTriggered = false;

        bool grounded = networkCharacterController == null || networkCharacterController.Grounded;
        SetBoolIfExists("isGrounded", grounded);

        float planarSpeed = 0f;
        if (networkCharacterController != null)
        {
            Vector3 v = networkCharacterController.Velocity;
            planarSpeed = new Vector2(v.x, v.z).magnitude;
        }

        animator.SetFloat("Speed", planarSpeed, 0.1f, Time.deltaTime);

        bool moving = controller.NetLocomotionState == SharedModePlayerController.LocomotionState.Moving ||
                      controller.NetLocomotionState == SharedModePlayerController.LocomotionState.Sprinting;
        bool jumping = controller.NetLocomotionState == SharedModePlayerController.LocomotionState.Jumping;
        bool falling = controller.NetLocomotionState == SharedModePlayerController.LocomotionState.Falling;

        if (jumping && !wasJumping)
        {
            SetTriggerIfExists("JumpTrigger");
        }
        wasJumping = jumping;

        SetBoolIfExists("isMove", moving);
        SetBoolIfExists("isIdle", !moving && !jumping && !falling);
        SetBoolIfExists("isFalling", falling);

        bool isBlocking = controller.IsBlocking;
        SetBoolIfExists("isBlocking", isBlocking);
        bool isAiming = controller.IsAiming;
        SetBoolIfExists("isAiming", isAiming);

        if (isAiming && !wasAiming)
        {
            ResetTriggerIfExists(StartAimTrigger);
            SetTriggerIfExists(StartAimTrigger);
        }
        else if (!isAiming && wasAiming)
        {
            bowHandVisualEvents?.AnimationEvent_DeactivateBowInHand();
        }
        wasAiming = isAiming;

        if (isBlocking && !wasBlocking)
        {
            SetTriggerIfExists("startBlock");
            SetIntegerIfExists("ComboStep", 0);
            ResetTriggerIfExists(AttackSwordTrigger);
            ResetTriggerIfExists(AttackBowTrigger);
            
        }
        wasBlocking = isBlocking;
        

        int attackSequence = controller.AttackSequence;
        if (lastAttackSequence < 0)
        {
            lastAttackSequence = attackSequence;
        }
        else if (attackSequence != lastAttackSequence)
        {
            lastAttackSequence = attackSequence;
            TriggerAttackByWeapon();
        }

        if (lastHitSequence != controller.HitSequence)
        {
            lastHitSequence = controller.HitSequence;
            ResetTriggerIfExists(AttackSwordTrigger);
            ResetTriggerIfExists(AttackBowTrigger);
            SetIntegerIfExists("ComboStep", 0);

            if (controller.NetCombatState == SharedModePlayerController.CombatState.BlockHit || isBlocking)
            {
                SetTriggerIfExists("BlockHit");
            }
            else
            {
                SetTriggerIfExists("GetHit");
            }
        }
    }

    private void TriggerAttackByWeapon()
    {
        bool useBowTrigger = controller.UsesBow && HasParameter(AttackBowTrigger, AnimatorControllerParameterType.Trigger);

        if (useBowTrigger)
        {
            ResetTriggerIfExists(AttackSwordTrigger);
            ResetTriggerIfExists(AttackBowTrigger);
            SetTriggerIfExists(AttackBowTrigger);
            return;
        }

        ResetTriggerIfExists(AttackSwordTrigger);
        SetTriggerIfExists(AttackSwordTrigger);
    }

    private void SetBoolIfExists(string parameterName, bool value)
    {
        if (HasParameter(parameterName, AnimatorControllerParameterType.Bool))
        {
            animator.SetBool(parameterName, value);
        }
    }

    private void SetIntegerIfExists(string parameterName, int value)
    {
        if (HasParameter(parameterName, AnimatorControllerParameterType.Int))
        {
            animator.SetInteger(parameterName, value);
        }
    }

    private void SetTriggerIfExists(string parameterName)
    {
        if (HasParameter(parameterName, AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger(parameterName);
        }
    }

    private void ResetTriggerIfExists(string parameterName)
    {
        if (HasParameter(parameterName, AnimatorControllerParameterType.Trigger))
        {
            animator.ResetTrigger(parameterName);
        }
    }

    private bool HasParameter(string parameterName, AnimatorControllerParameterType parameterType)
    {
        if (animator == null)
        {
            return false;
        }

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter p = parameters[i];
            if (p.name == parameterName && p.type == parameterType)
            {
                return true;
            }
        }

        return false;
    }
}
