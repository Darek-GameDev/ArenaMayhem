using UnityEngine;

[DisallowMultipleComponent]
public class SharedModeAnimatorBridge : MonoBehaviour
{
    [SerializeField] private SharedModePlayerController controller;
    [SerializeField] private Animator animator;
    [SerializeField] private Fusion.NetworkCharacterController networkCharacterController;

    private int lastHitSequence = -1;
    private bool deadTriggered;
    private bool wasBlocking;
    private bool wasJumping;

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
    }

    private void Update()
    {
        if (controller == null || animator == null)
        {
            return;
        }

        bool isDead = controller.IsDead;
        animator.SetBool("isDead", isDead);

        if (isDead)
        {
            if (!deadTriggered)
            {
                animator.SetTrigger("DeadTrigger");
                deadTriggered = true;
            }

            animator.SetBool("isMove", false);
            animator.SetBool("isIdle", true);
            animator.SetBool("isFalling", false);
            animator.SetBool("isBlocking", false);
            animator.SetFloat("Speed", 0f, 0.08f, Time.deltaTime);
            wasJumping = false;
            return;
        }

        deadTriggered = false;

        bool grounded = networkCharacterController == null || networkCharacterController.Grounded;
        animator.SetBool("isGrounded", grounded);

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
            animator.SetTrigger("JumpTrigger");
        }
        wasJumping = jumping;

        animator.SetBool("isMove", moving);
        animator.SetBool("isIdle", !moving && !jumping && !falling);
        animator.SetBool("isFalling", falling);

        bool isBlocking = controller.IsBlocking;
        animator.SetBool("isBlocking", isBlocking);

        if (isBlocking && !wasBlocking)
        {
            animator.SetTrigger("startBlock");
            animator.SetInteger("ComboStep", 0);
            animator.ResetTrigger("AttackSword");
            
        }
        wasBlocking = isBlocking;
        

        if (controller.AttackPressed)
        {
            animator.ResetTrigger("AttackSword");
            animator.SetTrigger("AttackSword");
        }

        if (lastHitSequence != controller.HitSequence)
        {
            lastHitSequence = controller.HitSequence;
            if (controller.NetCombatState == SharedModePlayerController.CombatState.BlockHit || isBlocking)
            {
                animator.SetTrigger("BlockHit");
            }
            else
            {
                animator.SetTrigger("GetHit");
            }
        }
    }
}
