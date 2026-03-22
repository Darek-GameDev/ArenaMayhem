using Fusion;
using UnityEngine;

public class PlayerAttack : NetworkBehaviour
{
    private Animator animator;
    [Networked] private NetworkButtons PreviousButtons { get; set; }
    [Networked] private NetworkBool IsBlocking { get; set; }
    [Networked] private int AttackSequence { get; set; }
    [Networked] private int BlockStartSequence { get; set; }
    [Networked] private int ComboStep { get; set; }
    [Networked] private int GetHitSequence { get; set; }

    private int renderedAttackSequence;
    private int renderedBlockStartSequence;
    private int renderedComboStep;
    private int renderedGetHitSequence;

    public override void Spawned()
    {
        EnsureAnimator();
        renderedAttackSequence = AttackSequence;
        renderedBlockStartSequence = BlockStartSequence;
        renderedComboStep = ComboStep;
        renderedGetHitSequence = GetHitSequence;
        animator.SetBool("isBlocking", IsBlocking);
        animator.SetInteger("ComboStep", ComboStep);
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
            ComboStep = 0;
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

        if (renderedComboStep != ComboStep)
        {
            renderedComboStep = ComboStep;
            animator.SetInteger("ComboStep", ComboStep);
        }

        if (renderedGetHitSequence != GetHitSequence)
        {
            renderedGetHitSequence = GetHitSequence;
            animator.SetTrigger("GetHit");
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
        if (!HasStateAuthority)
        {
            return;
        }

        ComboStep = step;
    }

    public void GetHit()
    {
        if (!HasStateAuthority)
        {
            return;
        }

        GetHitSequence++;
    }
}
