using Fusion;
using UnityEngine;

public class PlayerAttack : NetworkBehaviour
{
    private Animator animator;
    [Networked] private NetworkButtons PreviousButtons { get; set; }
    [Networked] private NetworkBool IsBlocking { get; set; }
    [Networked] private int AttackSequence { get; set; }
    [Networked] private int BlockStartSequence { get; set; }

    private int renderedAttackSequence;
    private int renderedBlockStartSequence;

    public override void Spawned()
    {
        EnsureAnimator();
        renderedAttackSequence = AttackSequence;
        renderedBlockStartSequence = BlockStartSequence;
        animator.SetBool("isBlocking", IsBlocking);
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
        {
            return;
        }

        PlayerNetworkInputData inputData = default;
        GetInput(out inputData);

        NetworkButtons pressed = inputData.Buttons.GetPressed(PreviousButtons);
        NetworkButtons released = inputData.Buttons.GetReleased(PreviousButtons);
        PreviousButtons = inputData.Buttons;

        if (pressed.IsSet((int)PlayerInputButton.Block))
        {
            IsBlocking = true;
            BlockStartSequence++;
        }

        if (released.IsSet((int)PlayerInputButton.Block))
        {
            IsBlocking = false;
        }

        if (!IsBlocking && pressed.IsSet((int)PlayerInputButton.Attack))
        {
            AttackSequence++;
        }
    }

    public override void Render()
    {
        EnsureAnimator();

        if (renderedBlockStartSequence != BlockStartSequence)
        {
            renderedBlockStartSequence = BlockStartSequence;
            animator.SetTrigger("startBlock");
            animator.ResetTrigger("AttackSword");
            animator.SetInteger("ComboStep", 0);
        }

        if (renderedAttackSequence != AttackSequence)
        {
            renderedAttackSequence = AttackSequence;
            animator.ResetTrigger("AttackSword");
            animator.SetTrigger("AttackSword");
        }

        animator.SetBool("isBlocking", IsBlocking);
    }

    private void EnsureAnimator()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    public void SetComboStep(int step)
    {
        animator.SetInteger("ComboStep", step);
    }

    public void GetHit()
    {
        // Trigger hit animation while maintaiing blocking state
        animator.SetTrigger("GetHit");
        // isBlocking remains true if the player is still holding right click
    }
}
