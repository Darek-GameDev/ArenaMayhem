using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
public class SharedModeEnemyAnimatorBridge : MonoBehaviour
{
    [SerializeField] private NetworkEnemyController controller;
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private Animator animator;
    [SerializeField] private NetworkCharacterController networkCharacterController;

    private int lastAttackSequence = -1;
    private int lastHitSequence = -1;
    private bool deadTriggered;
    private bool wasBlocking;

    private void Awake()
    {
        if (controller == null)
        {
            controller = GetComponent<NetworkEnemyController>();
        }

        if (enemyHealth == null)
        {
            enemyHealth = GetComponent<EnemyHealth>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (networkCharacterController == null)
        {
            networkCharacterController = GetComponent<NetworkCharacterController>();
        }
    }

    private void Update()
    {
        if (controller == null || animator == null || enemyHealth == null)
        {
            return;
        }

        bool isDead = enemyHealth.IsDead;
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
            animator.SetBool("isBlocking", false);
            animator.SetFloat("Speed", 0f, 0.1f, Time.deltaTime);
            return;
        }

        deadTriggered = false;

        float planarSpeed = 0f;
        if (networkCharacterController != null)
        {
            Vector3 velocity = networkCharacterController.Velocity;
            planarSpeed = new Vector2(velocity.x, velocity.z).magnitude;
        }

        bool moving = controller.NetLocomotionState == NetworkEnemyController.LocomotionState.Moving;
        animator.SetFloat("Speed", planarSpeed, 0.1f, Time.deltaTime);
        animator.SetBool("isMove", moving);
        animator.SetBool("isIdle", !moving);

        bool isBlocking = controller.IsBlocking;
        animator.SetBool("isBlocking", isBlocking);

        if (isBlocking && !wasBlocking)
        {
            animator.SetInteger("ComboStep", 0);
            animator.ResetTrigger("AttackSword");
            animator.SetTrigger("startBlock");
        }
        wasBlocking = isBlocking;

        animator.SetInteger("ComboStep", controller.ComboStep);

        int attackSequence = controller.AttackSequence;
        if (lastAttackSequence < 0)
        {
            lastAttackSequence = attackSequence;
        }
        else if (attackSequence != lastAttackSequence)
        {
            lastAttackSequence = attackSequence;
            animator.ResetTrigger("AttackSword");
            animator.SetTrigger("AttackSword");
        }

        int hitSequence = enemyHealth.HitSequence;
        if (lastHitSequence < 0)
        {
            lastHitSequence = hitSequence;
        }
        else if (hitSequence != lastHitSequence)
        {
            lastHitSequence = hitSequence;
            if (isBlocking)
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
