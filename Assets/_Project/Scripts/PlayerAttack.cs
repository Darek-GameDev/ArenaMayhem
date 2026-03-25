using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private int comboStep = 0;
    [SerializeField] private float blockCooldown = 0.25f;
    public bool IsBlocking => currentState == AttackState.Block;
    [SerializeField] private Weapon weapon;
    private PlayerHealth playerHealth;

    private Animator animator;
    private AttackState currentState = AttackState.Idle;
    private float lastBlockTime = -Mathf.Infinity;

    enum AttackState
    {
        Idle,
        Attack,
        Block,
        BlockHit,
    }

    void Start()
    {
        animator = GetComponent<Animator>();
        if (weapon == null)
        {
            weapon = GetComponentInChildren<Weapon>();
        }
        playerHealth = GetComponent<PlayerHealth>();
    }

    // Called from Animation Event to start the attack window.
    public void BeginAttackWindow()
    {
        if (weapon != null)
        {
            weapon.BeginAttackWindow();
        }
    }

    // Called from Animation Event to end the attack window.
    public void EndAttackWindow()
    {
        if (weapon != null)
        {
            weapon.EndAttackWindow();
        }
    }

    private void ChangeState(AttackState newState)
    {
        switch (currentState)
        {

            case AttackState.Block:
                animator.SetBool("isBlocking", false);
                break;
        }

        currentState = newState;

        switch (newState)
        {
            case AttackState.Idle:
                break;
            case AttackState.Attack:
                animator.ResetTrigger("AttackSword");
                animator.SetTrigger("AttackSword");
                break;
            case AttackState.Block:
                ResetCombo();
                animator.SetBool("isBlocking", true);
                animator.SetTrigger("startBlock");
                break;
            case AttackState.BlockHit:
                animator.SetTrigger("BlockHit");
                break;
        }

    }

    public void SetComboStep(int step)
    {
        comboStep = step;
        animator.SetInteger("ComboStep", comboStep);
    }
    public void ResetCombo()
    {
        comboStep = 0;
        animator.SetInteger("ComboStep", comboStep);
    }
    public void OnAttack(InputAction.CallbackContext context)
    {
        if(playerHealth != null && playerHealth.IsDead) return;
        if (context.performed)
        {
            if(currentState == AttackState.Block) return;
            
            ChangeState(AttackState.Attack);
        }
    }
    public void OnBlock(InputAction.CallbackContext context)
    {
        ResetCombo();
        if(playerHealth != null && playerHealth.IsDead) return;
        if (context.performed)
        {
            bool canBlock = (Time.time - lastBlockTime) >= blockCooldown;
            if (!canBlock) return;
            
            lastBlockTime = Time.time;
            ChangeState(AttackState.Block);

        }
        else if (context.canceled)
        {
            ChangeState(AttackState.Idle);
        }
    }
}
