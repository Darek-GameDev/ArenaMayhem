using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkCharacterController))]
public class SharedModePlayerController : NetworkBehaviour
{
    public enum PlayerWeaponType : byte
    {
        Sword = 0,
        Bow = 1,
    }

    public enum LocomotionState : byte
    {
        Idle = 0,
        Moving = 1,
        Sprinting = 2,
        Jumping = 3,
        Falling = 4,
    }

    public enum CombatState : byte
    {
        None = 0,
        Attacking = 1,
        Blocking = 2,
        BlockHit = 3,
        Aiming = 4,
    }

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float runSpeed = 5f;
    [SerializeField] private float lookYawSpeed = 0.15f;
    [SerializeField] private float instantAcceleration = 100f;
    [SerializeField] private float instantBraking = 100f;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private GameObject playerCameraRoot;

    [Header("Combat")]
    [SerializeField] private float blockCooldownSeconds = 0.25f;
    [SerializeField] private PlayerWeaponType weaponType = PlayerWeaponType.Sword;
    [SerializeField] private bool bowRequireAimToFire = true;
    [SerializeField] private bool bowAutoExitAimOnShoot = false;
    [SerializeField] private float bowFireCooldownSeconds = 0.35f;
    [SerializeField] private BowWeapon bowWeapon;

    [Header("Health")]
    [SerializeField] private int maxHealth = 10;

    [Networked] public PlayerHealthNetworkState NetHealthState { get; set; }
    [Networked] public NetworkBool IsBlocking { get; set; }
    [Networked] public NetworkBool AttackPressed { get; set; }
    [Networked] public int AttackSequence { get; set; }
    [Networked] public byte ComboStep { get; set; }
    [Networked] public int HitSequence { get; set; }
    [Networked] public int KillCount { get; set; }
    [Networked] public int CollectedItemCount { get; set; }
    [Networked] public LocomotionState NetLocomotionState { get; set; }
    [Networked] public CombatState NetCombatState { get; set; }
    [Networked] public float NextBlockAllowedAt { get; set; }
    [Networked] public float NextBowShotAllowedAt { get; set; }
    [Networked] public NetworkBool IsAiming { get; set; }
    [Networked] public NetworkBool BowRequireAimRelease { get; set; }

    private NetworkCharacterController cc;

    public PlayerWeaponType WeaponType => weaponType;
    public bool UsesBow => weaponType == PlayerWeaponType.Bow;

    public int Health
    {
        get => NetHealthState.Current;
        private set
        {
            PlayerHealthNetworkState state = EnsureValidHealthState(NetHealthState);
            state.Current = Mathf.Clamp(value, 0, state.Max);
            state.IsDead = state.Current <= 0;
            NetHealthState = state;
        }
    }

    public bool IsDead
    {
        get => NetHealthState.IsDead;
        private set
        {
            PlayerHealthNetworkState state = EnsureValidHealthState(NetHealthState);
            state.IsDead = value;
            if (value)
            {
                state.Current = 0;
            }

            NetHealthState = state;
        }
    }

    public int MaxHealth => EnsureValidHealthState(NetHealthState).Max;

    public override void Spawned()
    {
        cc = GetComponent<NetworkCharacterController>();
        cc.acceleration = instantAcceleration;
        cc.braking = instantBraking;
        cc.rotationSpeed = 0f;

        ConfigureCameraOwnership();
        EnsureWorldSpaceHealthBar();

        if (HasStateAuthority)
        {
            PlayerHealthNetworkState state = EnsureValidHealthState(default);
            state.Current = state.Max;
            state.IsDead = false;
            NetHealthState = state;
            IsBlocking = false;
            AttackPressed = false;
            AttackSequence = 0;
            ComboStep = 0;
            HitSequence = 0;
            KillCount = 0;
            CollectedItemCount = 0;
            NetLocomotionState = LocomotionState.Idle;
            NetCombatState = CombatState.None;
            NextBlockAllowedAt = 0f;
            NextBowShotAllowedAt = 0f;
            IsAiming = false;
            BowRequireAimRelease = false;
        }

        if (bowWeapon == null)
        {
            bowWeapon = GetComponentInChildren<BowWeapon>();
        }

        if (UsesBow && GetComponent<PlayerAimCameraController>() == null)
        {
            gameObject.AddComponent<PlayerAimCameraController>();
        }
    }

    private void ConfigureCameraOwnership()
    {
        GameObject cameraRoot = playerCameraRoot;
        if (cameraRoot == null)
        {
            Transform freeLook = transform.Find("FreeLook Camera");
            if (freeLook != null)
            {
                cameraRoot = freeLook.gameObject;
            }
        }

        bool isLocalPlayer = Object != null && Object.HasInputAuthority;

        if (cameraRoot != null)
        {
            cameraRoot.SetActive(isLocalPlayer);
            if (isLocalPlayer)
            {
                cameraTransform = cameraRoot.transform;
            }
        }

        if (isLocalPlayer && cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
        {
            return;
        }

        if (IsDead)
        {
            NetLocomotionState = LocomotionState.Idle;
            NetCombatState = CombatState.None;
            IsBlocking = false;
            IsAiming = false;
            AttackPressed = false;
            return;
        }

        if (!GetInput(out PlayerNetworkInput input))
        {
            return;
        }

        float yaw = input.Look.x * lookYawSpeed;
        if (Mathf.Abs(yaw) > 0.0001f)
        {
            transform.Rotate(0f, yaw, 0f);
        }

        bool sprintHeld = input.Buttons.IsSet((int)PlayerInputButton.Sprint);
        bool blockHeld = input.Buttons.IsSet((int)PlayerInputButton.Block);
        bool aimHeld = input.Buttons.IsSet((int)PlayerInputButton.Aim);

        cc.maxSpeed = sprintHeld ? runSpeed : walkSpeed;

        Vector3 worldDirection = GetWorldMoveDirection(input.Move);
        cc.Move(worldDirection);

        if (worldDirection.sqrMagnitude <= 0.0001f)
        {
            Vector3 velocity = cc.Velocity;
            velocity.x = 0f;
            velocity.z = 0f;
            cc.Velocity = velocity;
        }

        if (input.JumpPressed)
        {
            cc.Jump();
        }

        float simTime = (float)Runner.SimulationTime;

        if (UsesBow)
        {
            HandleBowCombat(input, aimHeld, simTime);
        }
        else
        {
            HandleSwordCombat(input, blockHeld, simTime);
        }

        UpdateLocomotionState(worldDirection, sprintHeld);

        if (worldDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = UsesBow && IsAiming
                ? Quaternion.LookRotation(GetAimForward(), Vector3.up)
                : Quaternion.LookRotation(worldDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 20f * Runner.DeltaTime);
        }
        else if (UsesBow && IsAiming)
        {
            Quaternion targetRotation = Quaternion.LookRotation(GetAimForward(), Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 20f * Runner.DeltaTime);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestDamage(int amount)
    {
        ApplyDamage(amount, default);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestDamageFromPlayer(int amount, PlayerRef attackerRef)
    {
        ApplyDamage(amount, attackerRef);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ResetAfterRespawn(Vector3 worldPosition)
    {
        transform.position = worldPosition;

        if (HasStateAuthority)
        {
            SetFullHealth();
            IsBlocking = false;
            IsAiming = false;
            AttackPressed = false;
            AttackSequence = 0;
            ComboStep = 0;
            HitSequence = 0;
            NetLocomotionState = LocomotionState.Idle;
            NetCombatState = CombatState.None;
            NextBlockAllowedAt = 0f;
            NextBowShotAllowedAt = 0f;
            BowRequireAimRelease = false;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestFullHeal()
    {
        if (IsDead)
        {
            return;
        }

        SetFullHealth();
        IsBlocking = false;
        IsAiming = false;
        AttackPressed = false;
        ComboStep = 0;
        BowRequireAimRelease = false;

        if (NetCombatState == CombatState.Attacking || NetCombatState == CombatState.BlockHit)
        {
            NetCombatState = CombatState.None;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestAddCollectedItem(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        CollectedItemCount += amount;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestAddKill(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        KillCount += amount;
    }

    private void ApplyDamage(int amount, PlayerRef attackerRef)
    {
        if (amount <= 0 || IsDead)
        {
            return;
        }

        HitSequence++;
        AttackPressed = false;
        ComboStep = 0;
        IsAiming = false;

        if (IsBlocking)
        {
            NetCombatState = CombatState.BlockHit;
            return;
        }

        Health = Health - amount;
        if (Health == 0)
        {
            IsDead = true;
            IsBlocking = false;
            IsAiming = false;
            NetCombatState = CombatState.None;
            NetLocomotionState = LocomotionState.Idle;

            if (attackerRef.IsRealPlayer && Object != null && Object.IsValid && attackerRef != Object.InputAuthority)
            {
                SharedModePlayerController attacker = ResolvePlayerController(attackerRef);
                if (attacker != null)
                {
                    attacker.RPC_RequestAddKill(1);
                }
            }
        }
    }

    private SharedModePlayerController ResolvePlayerController(PlayerRef playerRef)
    {
        if (Runner == null || !playerRef.IsRealPlayer)
        {
            return null;
        }

        NetworkObject playerObject = Runner.GetPlayerObject(playerRef);
        if (playerObject == null)
        {
            return null;
        }

        return playerObject.GetComponent<SharedModePlayerController>();
    }

    private void HandleSwordCombat(PlayerNetworkInput input, bool blockHeld, float simTime)
    {
        IsAiming = false;
        BowRequireAimRelease = false;

        // Drive block from held state to avoid dropped pressed/released transitions.
        if (blockHeld)
        {
            if (!IsBlocking && simTime >= NextBlockAllowedAt)
            {
                IsBlocking = true;
                NetCombatState = CombatState.Blocking;
                ComboStep = 0;
            }
        }
        else if (IsBlocking)
        {
            IsBlocking = false;
            NextBlockAllowedAt = simTime + blockCooldownSeconds;
            if (NetCombatState == CombatState.Blocking || NetCombatState == CombatState.BlockHit)
            {
                NetCombatState = CombatState.None;
            }
        }

        if (input.AttackPressed && !IsBlocking)
        {
            AttackPressed = true;
            AttackSequence++;
            NetCombatState = CombatState.Attacking;
            ComboStep = (byte)((ComboStep + 1) % 4);
        }
        else
        {
            AttackPressed = false;
            if (!IsBlocking && NetCombatState == CombatState.Attacking)
            {
                NetCombatState = CombatState.None;
            }
        }
    }

    private void HandleBowCombat(PlayerNetworkInput input, bool aimHeld, float simTime)
    {
        IsBlocking = false;
        ComboStep = 0;

        if (BowRequireAimRelease)
        {
            if (!aimHeld)
            {
                BowRequireAimRelease = false;
            }

            aimHeld = false;
        }

        IsAiming = aimHeld;

        if (IsAiming)
        {
            NetCombatState = CombatState.Aiming;
        }

        bool canFireNow = simTime >= NextBowShotAllowedAt;
        bool fireRequested = input.AttackPressed && (!bowRequireAimToFire || IsAiming);
        if (fireRequested && canFireNow)
        {
            AttackPressed = true;
            AttackSequence++;
            NetCombatState = CombatState.Attacking;
            NextBowShotAllowedAt = simTime + bowFireCooldownSeconds;

            FireBowProjectile();

            if (bowAutoExitAimOnShoot)
            {
                // Optional behavior: auto-return camera/aim to normal after each shot.
                IsAiming = false;
                BowRequireAimRelease = true;
            }
            else
            {
                // Keep aiming while the player continues to hold the aim button.
                IsAiming = aimHeld;
                BowRequireAimRelease = false;
            }
        }
        else
        {
            AttackPressed = false;
            if (!IsAiming && (NetCombatState == CombatState.Attacking || NetCombatState == CombatState.Aiming))
            {
                NetCombatState = CombatState.None;
            }
        }
    }

    private void FireBowProjectile()
    {
        if (bowWeapon == null)
        {
            bowWeapon = GetComponentInChildren<BowWeapon>();
        }

        if (bowWeapon == null)
        {
            return;
        }

        PlayerRef attackerRef = default;
        if (Object != null && Object.IsValid)
        {
            attackerRef = Object.InputAuthority;
        }

        bowWeapon.Fire(transform, attackerRef, GetAimDirection());
    }

    private Vector3 GetAimForward()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            return transform.forward;
        }

        // Get aim direction from screen center for consistent rotation
        Ray screenCenterRay = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 forward = screenCenterRay.direction;
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.0001f)
        {
            return transform.forward;
        }

        return forward.normalized;
    }

    private Vector3 GetAimDirection()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            return transform.forward;
        }

        // Get aim direction from screen center for proper projectile trajectory
        Ray screenCenterRay = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        return screenCenterRay.direction.normalized;
    }

    private void UpdateLocomotionState(Vector3 worldDirection, bool sprintHeld)
    {
        if (!cc.Grounded)
        {
            NetLocomotionState = cc.Velocity.y >= 0f ? LocomotionState.Jumping : LocomotionState.Falling;
            return;
        }

        if (worldDirection.sqrMagnitude <= 0.0001f)
        {
            NetLocomotionState = LocomotionState.Idle;
            return;
        }

        NetLocomotionState = sprintHeld ? LocomotionState.Sprinting : LocomotionState.Moving;
    }

    private PlayerHealthNetworkState EnsureValidHealthState(PlayerHealthNetworkState state)
    {
        if (state.Max <= 0)
        {
            state.Max = Mathf.Max(1, maxHealth);
        }

        state.Current = Mathf.Clamp(state.Current, 0, state.Max);
        if (state.Current == 0)
        {
            state.IsDead = true;
        }

        return state;
    }

    private void SetFullHealth()
    {
        PlayerHealthNetworkState state = EnsureValidHealthState(NetHealthState);
        state.Current = state.Max;
        state.IsDead = false;
        NetHealthState = state;
    }

    private Vector3 GetWorldMoveDirection(Vector2 inputMove)
    {
        Vector3 localMove = new Vector3(inputMove.x, 0f, inputMove.y);
        if (localMove.sqrMagnitude > 1f)
        {
            localMove.Normalize();
        }

        Transform reference = cameraTransform;
        if (reference == null && Camera.main != null)
        {
            reference = Camera.main.transform;
        }

        if (reference == null)
        {
            return transform.forward * localMove.z + transform.right * localMove.x;
        }

        Vector3 forward = reference.forward;
        Vector3 right = reference.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();
        return forward * localMove.z + right * localMove.x;
    }

    private void EnsureWorldSpaceHealthBar()
    {
        WorldSpaceHealthBar healthBar = GetComponent<WorldSpaceHealthBar>();
        if (healthBar == null)
        {
            healthBar = gameObject.AddComponent<WorldSpaceHealthBar>();
        }

        healthBar.ConfigureForPlayer(this, hideLocalInputAuthority: true);
    }
}
