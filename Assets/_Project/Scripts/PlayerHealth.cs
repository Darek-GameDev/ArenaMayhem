using UnityEngine;


public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private bool logDamage = true;
    [SerializeField] private PlayerAttack playerAttack;
    private int currentHealth;

    public int CurrentHealth => currentHealth;
    public bool IsDead => currentHealth <= 0;
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
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0)
        {
            return;
        }
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

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0)
        {
            return;
        }

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
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
                if(playerAttack.IsBlocking)
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
}