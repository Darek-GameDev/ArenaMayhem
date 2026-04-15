using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private int comboStep = 0;
    [SerializeField] private float blockCooldown = 0.25f;
    [SerializeField] private float blockDuration = 0.5f;
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
    [SerializeField] private float skillCooldown = 6f;
    [SerializeField] private float swordSkillDuration = 1.1f;
    [SerializeField] private float bowAimRayDistance = 200f;
    [SerializeField] private LayerMask bowAimLayerMask = ~0;

    private PlayerHealth playerHealth;

    private Animator animator;
    private AttackState currentState = AttackState.Idle;
    private float lastBlockTime = -Mathf.Infinity;
    private SharedModePlayerController sharedModeController;
    private CursorLockController cursorLockController;
    private float nextBowShotTime;
    private float nextSkillTime;
    private float swordSkillUntil;
    private bool isAiming;
    private bool bowRequireAimRelease;
    private float blockUntilTime;
    private bool blockRequireRelease;

    private const string AttackSwordTrigger = "AttackSword";
    private const string AttackBowTrigger = "AttackBow";
    private const string StartAimTrigger = "startAim";
    private const string SkillSwordTrigger = "SkillSpin";
    private const string UseSkillSwordBool = "useSkillSword";
    private const string AttackLayerName = "Attack";

    private int attackLayerIndex = -1;
    private bool attackLayerResolved;
    private float attackLayerDefaultWeight = 1f;
    private bool attackLayerWeightCached;
    private bool isAttackLayerSuppressed;

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
        CacheAttackLayer();
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
        cursorLockController = GetComponent<CursorLockController>();
        if (cursorLockController == null)
        {
            cursorLockController = gameObject.AddComponent<CursorLockController>();
        }

        if (sharedModeController == null)
        {
            cursorLockController.SetActiveForLocalPlayer(true);
        }
    }

    private void Update()
    {
        if (sharedModeController != null)
        {
            return;
        }

        if (playerHealth != null && playerHealth.IsFrozen)
        {
            if (currentState != AttackState.Idle)
            {
                ChangeState(AttackState.Idle);
            }

            SetBoolIfExists(UseSkillSwordBool, false);
            SetAttackLayerSuppressed(false);

            return;
        }

        if (Time.time >= swordSkillUntil)
        {
            SetBoolIfExists(UseSkillSwordBool, false);
            SetAttackLayerSuppressed(false);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            HandleSkillInput();
        }

        if (currentState == AttackState.Block && Time.time >= blockUntilTime)
        {
            ChangeState(AttackState.Idle);
            lastBlockTime = Time.time;
            blockRequireRelease = true;
        }
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
        if (playerHealth != null && playerHealth.IsFrozen) return;
        if (Time.time < swordSkillUntil) return;

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
        if (playerHealth != null && playerHealth.IsFrozen) return;
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
            ResetTriggerIfExists(StartAimTrigger);
            SetBoolIfExists("isAiming", false);
        }
    }

    public void OnBlock(InputAction.CallbackContext context)
    {
        if (sharedModeController != null) return;
        if (useBow) return;

        ResetCombo();
        if(playerHealth != null && playerHealth.IsDead) return;
        if (playerHealth != null && playerHealth.IsFrozen) return;
        if (Time.time < swordSkillUntil) return;
        if (context.performed)
        {
            if (blockRequireRelease) return;

            bool canBlock = (Time.time - lastBlockTime) >= blockCooldown;
            if (!canBlock) return;
            
            blockUntilTime = Time.time + blockDuration;
            ChangeState(AttackState.Block);

        }
        else if (context.canceled)
        {
            lastBlockTime = Time.time;
            blockRequireRelease = false;
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

        FireBowShot(false);
    }

    private void HandleSkillInput()
    {
        if (playerHealth != null && playerHealth.IsDead)
        {
            SetAttackLayerSuppressed(false);
            return;
        }

        if (Time.time < nextSkillTime)
        {
            return;
        }

        nextSkillTime = Time.time + skillCooldown;

        if (useBow)
        {
            FireBowShot(true);
            return;
        }

        swordSkillUntil = Time.time + Mathf.Max(0.05f, swordSkillDuration);
        SetBoolIfExists(UseSkillSwordBool, true);
        SetAttackLayerSuppressed(true);
        ResetCombo();
        if (HasParameter(SkillSwordTrigger, AnimatorControllerParameterType.Trigger))
        {
            ResetTriggerIfExists(AttackSwordTrigger);
            SetTriggerIfExists(SkillSwordTrigger);
        }
        else
        {
            ChangeState(AttackState.Attack);
        }
    }

    private bool FireBowShot(bool spawnSkillEffect)
    {
        if (Time.time < nextBowShotTime)
        {
            return false;
        }

        nextBowShotTime = Time.time + bowFireCooldown;
        ChangeState(AttackState.Attack);

        if (bowWeapon == null)
        {
            bowWeapon = GetComponentInChildren<BowWeapon>();
        }

        if (bowWeapon != null)
        {
            Vector3 aimPoint = BowWeapon.GetAimPointFromCamera(transform, bowAimRayDistance, bowAimLayerMask);
            if (spawnSkillEffect)
            {
                bowWeapon.FireSkillShot(transform, default, aimPoint);
            }
            else
            {
                bowWeapon.Fire(transform, default, aimPoint);
            }
        }

        if (autoExitAimOnShoot)
        {
            isAiming = false;
            bowRequireAimRelease = true;
            SetBoolIfExists("isAiming", false);
        }

        ChangeState(AttackState.Idle);
        return true;
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

    private void OnDisable()
    {
        SetAttackLayerSuppressed(false);
    }

    private void CacheAttackLayer()
    {
        if (animator == null || attackLayerResolved)
        {
            return;
        }

        attackLayerIndex = animator.GetLayerIndex(AttackLayerName);
        attackLayerResolved = true;
        if (attackLayerIndex >= 0)
        {
            attackLayerDefaultWeight = animator.GetLayerWeight(attackLayerIndex);
            attackLayerWeightCached = true;
        }
    }

    private void SetAttackLayerSuppressed(bool suppressed)
    {
        if (animator == null)
        {
            return;
        }

        CacheAttackLayer();
        if (attackLayerIndex < 0)
        {
            return;
        }

        if (!attackLayerWeightCached)
        {
            attackLayerDefaultWeight = animator.GetLayerWeight(attackLayerIndex);
            attackLayerWeightCached = true;
        }

        if (isAttackLayerSuppressed == suppressed)
        {
            return;
        }

        animator.SetLayerWeight(attackLayerIndex, suppressed ? 0f : attackLayerDefaultWeight);
        isAttackLayerSuppressed = suppressed;
    }

}
