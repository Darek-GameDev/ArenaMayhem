using UnityEngine;


public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private bool logDamage = true;
    [SerializeField] [Range(-1f, 1f)] private float blockFrontDotThreshold = 0.5f;
    [SerializeField] private PlayerAttack playerAttack;
    private int currentHealth;
    private SharedModePlayerController sharedModeController;
    private bool lastHitWasBlocked;
    private float frozenUntil;

    public int CurrentHealth => sharedModeController != null ? sharedModeController.Health : currentHealth;
    public int MaxHealth => sharedModeController != null ? sharedModeController.MaxHealth : maxHealth;
    public bool IsDead => sharedModeController != null ? sharedModeController.IsDead : currentHealth <= 0;
    public bool IsFrozen => sharedModeController != null ? sharedModeController.IsFrozen : Time.time < frozenUntil;
    private Animator animator;
    private HealthState currentState;
    enum HealthState
    {
        Dead,
        Hit,
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponent<Animator>();
        playerAttack = GetComponent<PlayerAttack>();
        sharedModeController = GetComponent<SharedModePlayerController>();
        currentHealth = maxHealth;
        frozenUntil = 0f;
    }

    public void TakeDamage(int amount)
    {
        TakeDamageInternal(amount, default, false);
    }

    public void TakeDamageFromOrigin(int amount, Vector3 hitOrigin)
    {
        TakeDamageInternal(amount, hitOrigin, true);
    }

    private void TakeDamageInternal(int amount, Vector3 hitOrigin, bool hasHitOrigin)
    {
        if (sharedModeController != null)
        {
            if (hasHitOrigin)
            {
                sharedModeController.RPC_RequestDamageWithOrigin(amount, hitOrigin);
            }
            else
            {
                sharedModeController.RPC_RequestDamage(amount);
            }

            return;
        }

        if (IsDead || amount <= 0)
        {
            return;
        }

        bool blockedHit = playerAttack != null && playerAttack.IsBlocking && CanBlockIncomingHit(hitOrigin, hasHitOrigin);
        if (blockedHit)
        {
            lastHitWasBlocked = true;
            ChangeState(HealthState.Hit);
            return;
        }

        lastHitWasBlocked = false;
        ChangeState(HealthState.Hit);
        currentHealth = Mathf.Max(0, currentHealth - amount);

        if (logDamage)
        {
            Debug.Log($"{name} took {amount} damage. HP: {currentHealth}/{maxHealth}");
        }

        if (currentHealth == 0)
        {
            Die();
        }
    }

    public void ApplyFreeze(float duration)
    {
        if (duration <= 0f || IsDead)
        {
            return;
        }

        if (sharedModeController != null)
        {
            sharedModeController.RequestFreeze(duration);
            return;
        }

        frozenUntil = Mathf.Max(frozenUntil, Time.time + duration);
    }

    public void Heal(int amount)
    {
        if (sharedModeController != null)
        {
            return;
        }

        if (IsDead || amount <= 0)
        {
            return;
        }

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    public void ResetHealth()
    {
        if (sharedModeController != null)
        {
            return;
        }

        currentHealth = maxHealth;
        frozenUntil = 0f;
    }

    private void Die()
    {
        if (logDamage)
        {
            Debug.Log($"{name} is dead.");
        }
    }
    private void ChangeState(HealthState newState)
    {
        switch (currentState)
        {
            case HealthState.Hit:
            
                break;
        }

        currentState = newState;

        switch (newState)
        {
            case HealthState.Hit:
                bool isBlocking = lastHitWasBlocked || (playerAttack != null && playerAttack.IsBlocking);
                if (playerAttack != null)
                {
                    playerAttack.ResetCombo();
                }

                animator.ResetTrigger("AttackSword");
                animator.SetInteger("ComboStep", 0);

                if(isBlocking)
                {
                    animator.SetTrigger("BlockHit");
                }
                else
                {
                    animator.SetTrigger("GetHit");
                }

                lastHitWasBlocked = false;
                break;
            case HealthState.Dead:
                animator.SetTrigger("DeadTrigger");
                animator.SetBool("isDead", true);
                break;
        }
    }

    private bool CanBlockIncomingHit(Vector3 hitOrigin, bool hasHitOrigin)
    {
        if (!hasHitOrigin)
        {
            return true;
        }

        Vector3 toHitOrigin = hitOrigin - transform.position;
        toHitOrigin.y = 0f;
        if (toHitOrigin.sqrMagnitude <= 0.0001f)
        {
            return true;
        }

        Vector3 defenderForward = transform.forward;
        defenderForward.y = 0f;
        defenderForward.Normalize();

        float dot = Vector3.Dot(defenderForward, toHitOrigin.normalized);
        return dot >= blockFrontDotThreshold;
    }
}