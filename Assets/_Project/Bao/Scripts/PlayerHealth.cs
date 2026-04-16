using UnityEngine;


public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private bool logDamage = true;
    [SerializeField] [Range(-1f, 1f)] private float blockFrontDotThreshold = 0.5f;
    [SerializeField] private PlayerAttack playerAttack;
    private int currentHealth;
    private SharedModePlayerController sharedModeController;

    public int CurrentHealth => sharedModeController != null ? sharedModeController.Health : currentHealth;
    public int MaxHealth => sharedModeController != null ? sharedModeController.MaxHealth : maxHealth;
    public bool IsDead => sharedModeController != null ? sharedModeController.IsDead : currentHealth <= 0;
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
    }

    public void TakeDamage(int amount)
    {
        TakeDamageInternal(amount, default, false);
    }

    public void TakeDamageFromOrigin(int amount, Vector3 hitOrigin)
    {
        TakeDamageInternal(amount, hitOrigin, true);
    }

    public void TakeEnvironmentalDamage(int amount)
    {
        TakeEnvironmentalDamageInternal(amount, default, false);
    }

    public void TakeEnvironmentalDamageFromOrigin(int amount, Vector3 hitOrigin)
    {
        TakeEnvironmentalDamageInternal(amount, hitOrigin, true);
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
            ChangeState(HealthState.Hit, true);
            return;
        }

        ApplyDamage(amount, false);
    }

    private void TakeEnvironmentalDamageInternal(int amount, Vector3 hitOrigin, bool hasHitOrigin)
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

        ApplyDamage(amount, false);
    }

    private void ApplyDamage(int amount, bool blockedHit)
    {
        ChangeState(HealthState.Hit, blockedHit);
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
    }

    private void Die()
    {
        if (logDamage)
        {
            Debug.Log($"{name} is dead.");
        }
    }
    private void ChangeState(HealthState newState, bool blockedHit = false)
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
                if (playerAttack != null)
                {
                    playerAttack.ResetCombo();
                }

                animator.ResetTrigger("AttackSword");
                animator.SetInteger("ComboStep", 0);

                if(blockedHit)
                {
                    animator.SetTrigger("BlockHit");
                }
                else
                {
                    animator.SetTrigger("GetHit");
                }

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