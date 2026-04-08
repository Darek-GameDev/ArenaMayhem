using UnityEngine;

[DisallowMultipleComponent]
public class EnemyAnimationEvents : MonoBehaviour
{
    [SerializeField] private EnemyWeapon enemyWeapon;
    [SerializeField] private NetworkEnemyController enemyController;
    [SerializeField] private Animator animator;

    private void Awake()
    {
        if (enemyWeapon == null)
        {
            enemyWeapon = GetComponentInChildren<EnemyWeapon>();
        }

        if (enemyController == null)
        {
            enemyController = GetComponentInParent<NetworkEnemyController>();
        }

        if (animator == null)
        {
            animator = GetComponentInParent<Animator>();
        }
    }

    public void BeginAttackWindow()
    {
        if (enemyWeapon != null)
        {
            enemyWeapon.BeginAttackWindow();
        }
    }

    public void EndAttackWindow()
    {
        if (enemyWeapon != null)
        {
            enemyWeapon.EndAttackWindow();
        }
    }

    public void SetComboStep(int step)
    {
        if (enemyController != null)
        {
            enemyController.SetComboStepFromAnimationEvent(step);
        }

        if (animator != null)
        {
            animator.SetInteger("ComboStep", step);
        }
    }

    public void SetCombostep(int step)
    {
        SetComboStep(step);
    }

    public void ResetCombo()
    {
        SetComboStep(0);
        if (enemyController != null)
        {
            enemyController.ResetComboStepFromAnimationEvent();
        }
    }

    public void FireBowProjectile()
    {
        if (enemyController != null)
        {
            enemyController.ReleaseQueuedRangedShotFromAnimationEvent();
        }
    }
}
