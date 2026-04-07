using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private int comboStep = 0;
    [SerializeField] private float blockCooldown = 0.25f;
    public bool IsBlocking => currentState == AttackState.Block;
    public bool IsAiming => isAiming;
    public bool UsesBow => useBow;

    [Header("Weapon Mode")]
    [SerializeField] private bool useBow;

    [Header("Melee")]
    [SerializeField] private Weapon weapon;

    [Header("Bow")]
    [SerializeField] private BowWeapon bowWeapon;
    [SerializeField] private bool bowRequireAimToFire = true;
    [SerializeField] private float bowFireCooldown = 0.35f;
    [SerializeField] private bool autoExitAimOnShoot = true;

    private PlayerHealth playerHealth;

    private Animator animator;
    private AttackState currentState = AttackState.Idle;
    private float lastBlockTime = -Mathf.Infinity;
    private SharedModePlayerController sharedModeController;
    private float nextBowShotTime;
    private bool isAiming;
    private bool bowRequireAimRelease;

    private const string AttackSwordTrigger = "AttackSword";
    private const string AttackBowTrigger = "AttackBow";
    private const string StartAimTrigger = "startAim";

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

        if (bowWeapon == null)
        {
            bowWeapon = GetComponentInChildren<BowWeapon>();
        }

        if (useBow && GetComponent<PlayerAimCameraController>() == null)
        {
            gameObject.AddComponent<PlayerAimCameraController>();
        }

        playerHealth = GetComponent<PlayerHealth>();
        sharedModeController = GetComponent<SharedModePlayerController>();
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
                TriggerAttackByWeapon();
                break;
            case AttackState.Block:
                ResetCombo();
                SetBoolIfExists("isBlocking", true);
                SetTriggerIfExists("startBlock");
                break;
            case AttackState.BlockHit:
                SetTriggerIfExists("BlockHit");
                break;
        }

    }

    public void SetComboStep(int step)
    {
        comboStep = step;
        SetIntegerIfExists("ComboStep", comboStep);
    }
    public void ResetCombo()
    {
        comboStep = 0;
        SetIntegerIfExists("ComboStep", comboStep);
    }
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (sharedModeController != null) return;
        if(playerHealth != null && playerHealth.IsDead) return;

        if (useBow)
        {
            HandleBowAttackInput(context);
            return;
        }

        if (context.performed)
        {
            if(currentState == AttackState.Block) return;
            
            ChangeState(AttackState.Attack);
        }
    }

    public void OnAim(InputAction.CallbackContext context)
    {
        if (sharedModeController != null) return;
        if (playerHealth != null && playerHealth.IsDead) return;
        if (!useBow) return;

        if (context.performed)
        {
            if (bowRequireAimRelease)
            {
                return;
            }

            bool wasAiming = isAiming;
            isAiming = true;
            if (!wasAiming)
            {
                ResetTriggerIfExists(StartAimTrigger);
                SetTriggerIfExists(StartAimTrigger);
            }
            SetBoolIfExists("isAiming", true);
        }
        else if (context.canceled)
        {
            isAiming = false;
            bowRequireAimRelease = false;
            SetBoolIfExists("isAiming", false);
        }
    }

    public void OnBlock(InputAction.CallbackContext context)
    {
        if (sharedModeController != null) return;
        if (useBow) return;

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

    private void HandleBowAttackInput(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }

        if (bowRequireAimRelease)
        {
            return;
        }

        if (bowRequireAimToFire && !isAiming)
        {
            return;
        }

        if (Time.time < nextBowShotTime)
        {
            return;
        }

        nextBowShotTime = Time.time + bowFireCooldown;
        ChangeState(AttackState.Attack);

        if (bowWeapon == null)
        {
            bowWeapon = GetComponentInChildren<BowWeapon>();
        }

        if (bowWeapon != null)
        {
            bowWeapon.Fire(transform, default);
        }

        if (autoExitAimOnShoot)
        {
            isAiming = false;
            bowRequireAimRelease = true;
            SetBoolIfExists("isAiming", false);
            ChangeState(AttackState.Idle);
        }
    }

    private void TriggerAttackByWeapon()
    {
        if (animator == null)
        {
            return;
        }

        bool useBowTrigger = useBow && HasParameter(AttackBowTrigger, AnimatorControllerParameterType.Trigger);

        if (useBowTrigger)
        {
            ResetTriggerIfExists(AttackSwordTrigger);
            ResetTriggerIfExists(AttackBowTrigger);
            SetTriggerIfExists(AttackBowTrigger);
            return;
        }

        ResetTriggerIfExists(AttackSwordTrigger);
        SetTriggerIfExists(AttackSwordTrigger);
    }

    private void SetBoolIfExists(string parameterName, bool value)
    {
        if (HasParameter(parameterName, AnimatorControllerParameterType.Bool))
        {
            animator.SetBool(parameterName, value);
        }
    }

    private void SetIntegerIfExists(string parameterName, int value)
    {
        if (HasParameter(parameterName, AnimatorControllerParameterType.Int))
        {
            animator.SetInteger(parameterName, value);
        }
    }

    private void SetTriggerIfExists(string parameterName)
    {
        if (HasParameter(parameterName, AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger(parameterName);
        }
    }

    private void ResetTriggerIfExists(string parameterName)
    {
        if (HasParameter(parameterName, AnimatorControllerParameterType.Trigger))
        {
            animator.ResetTrigger(parameterName);
        }
    }

    private bool HasParameter(string parameterName, AnimatorControllerParameterType parameterType)
    {
        if (animator == null)
        {
            return false;
        }

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = parameters[i];
            if (parameter.name == parameterName && parameter.type == parameterType)
            {
                return true;
            }
        }

        return false;
    }
}
