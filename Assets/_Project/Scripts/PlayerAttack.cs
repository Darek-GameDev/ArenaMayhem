using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    private Animator animator;
    private PlayerInput playerInput;
    private InputAction blockAction;
    private bool isBlocking = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponent<Animator>();
        playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            blockAction = playerInput.actions["Block"];
        }
    }

    void Update()
    {
        SyncBlockState();
    }
    
public void SetComboStep(int step)
    {
        animator.SetInteger("ComboStep", step);
    }
    private void OnAttack(InputValue value)
    {
        if (value == null || !value.isPressed)
        {
            return;
        }
        animator.ResetTrigger("AttackSword"); // Reset the trigger to allow re-triggering the animation
        animator.SetTrigger("AttackSword");
    }

    private void OnBlock(InputValue value)
    {
        if (value == null)
        {
            return;
        }

        isBlocking = value.isPressed;
        if(value.isPressed)
        {
            animator.SetTrigger("startBlock");
            animator.ResetTrigger("AttackSword"); // Ensure attack animation is not triggered while blocking    
            animator.SetInteger("ComboStep", 0); // Reset combo step when starting to block
        }
        animator.SetBool("isBlocking", isBlocking);
        
    }

    private void SyncBlockState()
    {
        if (blockAction == null)
        {
            return;
        }

        bool newBlockingState = blockAction.ReadValue<float>() > 0.5f;
        if (newBlockingState == isBlocking)
        {
            return;
        }

        isBlocking = newBlockingState;
        animator.SetBool("isBlocking", isBlocking);
    }

    public void GetHit()
    {
        // Trigger hit animation while maintaining blocking state
        animator.SetTrigger("GetHit");
        // isBlocking remains true if the player is still holding right click
    }
}
