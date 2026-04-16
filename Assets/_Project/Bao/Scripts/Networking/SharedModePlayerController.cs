using Fusion;
using UnityEngine;
using UnityEngine.UI;

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
        Skill = 5,
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
    [SerializeField] private float blockDurationSeconds = 0.5f;
    [SerializeField] [Range(-1f, 1f)] private float blockFrontDotThreshold = 0.5f;
    [SerializeField] private PlayerWeaponType weaponType = PlayerWeaponType.Sword;
    [SerializeField] private bool bowRequireAimToFire = true;
    [SerializeField] private bool bowAutoExitAimOnShoot = false;
    [SerializeField] private float bowFireCooldownSeconds = 0.35f;
    [SerializeField] private float skillCooldownSeconds = 6f;
    [SerializeField] private float swordSkillDurationSeconds = 1.1f;
    [SerializeField] private float bowMoveSpeedMultiplier = 1.2f;
    [SerializeField] private float bowAimMoveSpeedMultiplier = 0.65f;
    [SerializeField] private BowWeapon bowWeapon;
    [SerializeField] private BowWeapon.ProjectileType defaultBowProjectileType = BowWeapon.ProjectileType.Primary;
    [SerializeField] private float bowAimRayDistance = 200f;
    [SerializeField] private LayerMask bowAimLayerMask = ~0;

    [Header("Health")]
    [SerializeField] private int maxHealth = 10;

    [Header("Minimap Icon")]
    [SerializeField] private Sprite localPlayerMinimapSprite;
    [SerializeField] private Sprite enemyMinimapSprite;
    [SerializeField] private string minimapCanvasName = "CanvasIconPlayer";
    [SerializeField] private string minimapIconName = "Icon";

    [Networked] public PlayerHealthNetworkState NetHealthState { get; set; }
    [Networked] public NetworkBool IsBlocking { get; set; }
    [Networked] public NetworkBool AttackPressed { get; set; }
    [Networked] public int AttackSequence { get; set; }
    [Networked] public int SkillSequence { get; set; }
    [Networked] public byte ComboStep { get; set; }
    [Networked] public int HitSequence { get; set; }
    [Networked] public int KillCount { get; set; }
    [Networked] public int CollectedItemCount { get; set; }
    [Networked] public LocomotionState NetLocomotionState { get; set; }
    [Networked] public CombatState NetCombatState { get; set; }
    [Networked] public float NextBlockAllowedAt { get; set; }
    [Networked] public float BlockUntil { get; set; }
    [Networked] public float NextBowShotAllowedAt { get; set; }
    [Networked] public float NextSkillAllowedAt { get; set; }
    [Networked] public float SwordSkillUntil { get; set; }
    [Networked] public float FrozenUntil { get; set; }
    [Networked] public int FreezeSequence { get; set; }
    [Networked] public NetworkBool IsAiming { get; set; }
    [Networked] public NetworkBool BowRequireAimRelease { get; set; }
    [Networked] public NetworkBool BlockRequireRelease { get; set; }
    [Networked] public NetworkBool IsFreezeShotPrimed { get; set; }
    [Networked] public NetworkBool UseSkillSword { get; set; }
    [Networked] public BowWeapon.ProjectileType SelectedBowProjectileType { get; set; }

    private NetworkCharacterController cc;
    private CursorLockController cursorLockController;
    private Image minimapIconImage;
    private RawImage minimapIconRawImage;
    private bool minimapIconResolved;
    private bool minimapIconApplied;
    private bool lastMinimapWasLocal;

    public PlayerWeaponType WeaponType => weaponType;
    public bool UsesBow => weaponType == PlayerWeaponType.Bow;
    public bool IsFrozen => Runner != null && (float)Runner.SimulationTime < FrozenUntil;

    public void ApplySpawnClass(SharedPlayerClassType classType)
    {
        switch (classType)
        {
            case SharedPlayerClassType.Archer:
                weaponType = PlayerWeaponType.Bow;
                break;
            case SharedPlayerClassType.Knight:
                weaponType = PlayerWeaponType.Sword;
                break;
        }
    }

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
        if (HasStateAuthority && Object != null && Object.InputAuthority.IsRealPlayer)
        {
            if (SharedRoomSessionManager.TryGetPlayerClass(Runner, Object.InputAuthority, out SharedPlayerClassType selectedClass))
            {
                ApplySpawnClass(selectedClass);
            }
        }

        cc = GetComponent<NetworkCharacterController>();
        cc.acceleration = instantAcceleration;
        cc.braking = instantBraking;
        cc.rotationSpeed = 0f;

        ConfigureCameraOwnership();
        EnsureWorldSpaceHealthBar();

        cursorLockController = GetComponent<CursorLockController>();
        if (cursorLockController == null)
        {
            cursorLockController = gameObject.AddComponent<CursorLockController>();
        }

        cursorLockController.SetActiveForLocalPlayer(Object != null && Object.HasInputAuthority);

        if (HasStateAuthority)
        {
            PlayerHealthNetworkState state = EnsureValidHealthState(default);
            state.Current = state.Max;
            state.IsDead = false;
            NetHealthState = state;
            IsBlocking = false;
            AttackPressed = false;
            AttackSequence = 0;
            SkillSequence = 0;
            ComboStep = 0;
            HitSequence = 0;
            KillCount = 0;
            CollectedItemCount = 0;
            NetLocomotionState = LocomotionState.Idle;
            NetCombatState = CombatState.None;
            NextBlockAllowedAt = 0f;
            BlockUntil = 0f;
            NextBowShotAllowedAt = 0f;
            NextSkillAllowedAt = 0f;
            SwordSkillUntil = 0f;
            FrozenUntil = 0f;
            FreezeSequence = 0;
            IsAiming = false;
            BowRequireAimRelease = false;
            BlockRequireRelease = false;
            IsFreezeShotPrimed = false;
            UseSkillSword = false;
            SelectedBowProjectileType = defaultBowProjectileType;
        }

        if (bowWeapon == null)
        {
            bowWeapon = GetComponentInChildren<BowWeapon>();
        }

        if (UsesBow && GetComponent<PlayerAimCameraController>() == null)
        {
            gameObject.AddComponent<PlayerAimCameraController>();
        }

        RefreshMinimapIcon(forceRefresh: true);
    }

    private void Update()
    {
        RefreshMinimapIcon(forceRefresh: false);
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

    private void RefreshMinimapIcon(bool forceRefresh)
    {
        bool isLocal = Object != null && Object.IsValid && Object.HasInputAuthority;

        if (!forceRefresh && minimapIconApplied && lastMinimapWasLocal == isLocal)
        {
            return;
        }

        ResolveMinimapIconGraphic();
        if (!minimapIconResolved)
        {
            return;
        }

        if (localPlayerMinimapSprite == null && minimapIconImage != null && minimapIconImage.sprite != null)
        {
            localPlayerMinimapSprite = minimapIconImage.sprite;
        }

        Sprite targetSprite = isLocal ? localPlayerMinimapSprite : enemyMinimapSprite;
        if (targetSprite == null)
        {
            return;
        }

        if (minimapIconImage != null)
        {
            minimapIconImage.sprite = targetSprite;
        }

        if (minimapIconRawImage != null)
        {
            ApplySpriteToRawImage(minimapIconRawImage, targetSprite);
        }

        minimapIconApplied = true;
        lastMinimapWasLocal = isLocal;
    }

    private void ResolveMinimapIconGraphic()
    {
        if (minimapIconResolved && (minimapIconImage != null || minimapIconRawImage != null))
        {
            return;
        }

        Transform iconTransform = null;

        if (!string.IsNullOrWhiteSpace(minimapCanvasName))
        {
            Transform canvasTransform = transform.Find(minimapCanvasName);
            if (canvasTransform != null && !string.IsNullOrWhiteSpace(minimapIconName))
            {
                iconTransform = canvasTransform.Find(minimapIconName);
            }

            if (iconTransform == null && canvasTransform != null)
            {
                iconTransform = canvasTransform;
            }
        }

        if (iconTransform == null)
        {
            Image[] allImages = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < allImages.Length; i++)
            {
                Image candidate = allImages[i];
                if (candidate != null && candidate.gameObject.name == minimapIconName)
                {
                    minimapIconImage = candidate;
                    minimapIconResolved = true;
                    return;
                }
            }

            RawImage[] allRawImages = GetComponentsInChildren<RawImage>(true);
            for (int i = 0; i < allRawImages.Length; i++)
            {
                RawImage candidate = allRawImages[i];
                if (candidate != null && candidate.gameObject.name == minimapIconName)
                {
                    minimapIconRawImage = candidate;
                    minimapIconResolved = true;
                    return;
                }
            }

            return;
        }

        minimapIconImage = iconTransform.GetComponent<Image>();
        minimapIconRawImage = iconTransform.GetComponent<RawImage>();
        minimapIconResolved = minimapIconImage != null || minimapIconRawImage != null;
    }

    private static void ApplySpriteToRawImage(RawImage rawImage, Sprite sprite)
    {
        if (rawImage == null || sprite == null || sprite.texture == null)
        {
            return;
        }

        rawImage.texture = sprite.texture;

        Rect rect = sprite.textureRect;
        Texture texture = sprite.texture;
        rawImage.uvRect = new Rect(
            rect.x / texture.width,
            rect.y / texture.height,
            rect.width / texture.width,
            rect.height / texture.height);
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
            UseSkillSword = false;
            return;
        }

        float simTime = (float)Runner.SimulationTime;
        if (IsFrozenAt(simTime))
        {
            NetLocomotionState = LocomotionState.Idle;
            NetCombatState = CombatState.None;
            IsBlocking = false;
            IsAiming = false;
            AttackPressed = false;
            UseSkillSword = false;
            cc.Move(Vector3.zero);

            Vector3 frozenVelocity = cc.Velocity;
            frozenVelocity.x = 0f;
            frozenVelocity.z = 0f;
            cc.Velocity = frozenVelocity;
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
        float swordBaseSpeed = sprintHeld ? runSpeed : walkSpeed;
        if (UsesBow)
        {
            float speedMultiplier = aimHeld ? bowAimMoveSpeedMultiplier : bowMoveSpeedMultiplier;
            cc.maxSpeed = swordBaseSpeed * Mathf.Max(0f, speedMultiplier);
        }
        else
        {
            cc.maxSpeed = swordBaseSpeed;
        }

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

        if (IsBlocking && simTime >= BlockUntil)
        {
            IsBlocking = false;
            BlockRequireRelease = true;
            NextBlockAllowedAt = simTime + blockCooldownSeconds;
            if (NetCombatState == CombatState.Blocking || NetCombatState == CombatState.BlockHit)
            {
                NetCombatState = CombatState.None;
            }
        }

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
        ApplyDamage(amount, default, default, false);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestDamageWithOrigin(int amount, Vector3 hitOrigin)
    {
        ApplyDamage(amount, default, hitOrigin, true);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestDamageFromPlayer(int amount, PlayerRef attackerRef)
    {
        ApplyDamage(amount, attackerRef, default, false);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestDamageFromPlayerWithOrigin(int amount, PlayerRef attackerRef, Vector3 hitOrigin)
    {
        ApplyDamage(amount, attackerRef, hitOrigin, true);
    }

    public void RequestFreeze(float duration)
    {
        if (duration <= 0f || IsDead)
        {
            return;
        }

        if (HasStateAuthority)
        {
            ApplyFreeze(duration);
        }
        else
        {
            RPC_RequestFreeze(duration);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestFreeze(float duration)
    {
        if (duration <= 0f || IsDead)
        {
            return;
        }

        ApplyFreeze(duration);
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
            SkillSequence = 0;
            ComboStep = 0;
            HitSequence = 0;
            NetLocomotionState = LocomotionState.Idle;
            NetCombatState = CombatState.None;
            NextBlockAllowedAt = 0f;
            BlockUntil = 0f;
            NextBowShotAllowedAt = 0f;
            NextSkillAllowedAt = 0f;
            SwordSkillUntil = 0f;
            FrozenUntil = 0f;
            FreezeSequence = 0;
            BowRequireAimRelease = false;
            BlockRequireRelease = false;
            IsFreezeShotPrimed = false;
            UseSkillSword = false;
            SelectedBowProjectileType = defaultBowProjectileType;
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
        SkillSequence = 0;
        ComboStep = 0;
        BowRequireAimRelease = false;
        BlockRequireRelease = false;
        NextSkillAllowedAt = 0f;
        SwordSkillUntil = 0f;
        FrozenUntil = 0f;
        FreezeSequence = 0;
        IsFreezeShotPrimed = false;
        UseSkillSword = false;
        SelectedBowProjectileType = defaultBowProjectileType;

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

    private void ApplyDamage(int amount, PlayerRef attackerRef, Vector3 hitOrigin, bool hasHitOrigin)
    {
        if (amount <= 0 || IsDead)
        {
            return;
        }

        HitSequence++;
        AttackPressed = false;
        ComboStep = 0;
        IsAiming = false;

        if (IsBlocking && CanBlockIncomingHit(hitOrigin, hasHitOrigin))
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
        IsFreezeShotPrimed = false;

        if (UseSkillSword && simTime >= SwordSkillUntil)
        {
            UseSkillSword = false;
            SwordSkillUntil = 0f;
            if (NetCombatState == CombatState.Skill)
            {
                NetCombatState = CombatState.None;
            }
        }

        // Drive block from held state to avoid dropped pressed/released transitions.
        if (blockHeld)
        {
            if (!IsBlocking && !BlockRequireRelease && simTime >= NextBlockAllowedAt)
            {
                IsBlocking = true;
                BlockUntil = simTime + blockDurationSeconds;
                NetCombatState = CombatState.Blocking;
                ComboStep = 0;
            }
        }
        else
        {
            BlockRequireRelease = false;
            if (IsBlocking)
            {
                IsBlocking = false;
                NextBlockAllowedAt = simTime + blockCooldownSeconds;
                if (NetCombatState == CombatState.Blocking || NetCombatState == CombatState.BlockHit)
                {
                    NetCombatState = CombatState.None;
                }
            }
        }

        if (input.SkillPressed && simTime >= NextSkillAllowedAt)
        {
            SkillSequence++;
            NetCombatState = CombatState.Skill;
            NextSkillAllowedAt = simTime + skillCooldownSeconds;
            SwordSkillUntil = simTime + Mathf.Max(0.05f, swordSkillDurationSeconds);
            UseSkillSword = true;
        }
        else if (NetCombatState == CombatState.Skill && !UseSkillSword)
        {
            NetCombatState = CombatState.None;
        }

        if (UseSkillSword)
        {
            AttackPressed = false;
            return;
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
        UseSkillSword = false;
        SwordSkillUntil = 0f;

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

        if (input.WeaponSlot1Pressed)
        {
            SelectedBowProjectileType = BowWeapon.ProjectileType.Primary;
        }

        if (input.WeaponSlot2Pressed)
        {
            SelectedBowProjectileType = BowWeapon.ProjectileType.Secondary;
        }

        bool skillRequested = input.SkillPressed && simTime >= NextSkillAllowedAt && (!bowRequireAimToFire || IsAiming);
        bool canFireNow = simTime >= NextBowShotAllowedAt;
        bool primaryPressed = input.AttackPressed;
        bool fireRequested = primaryPressed && (!bowRequireAimToFire || IsAiming);

        if (skillRequested)
        {
            SkillSequence++;
            AttackSequence++;
            AttackPressed = true;
            NetCombatState = CombatState.Attacking;
            NextSkillAllowedAt = simTime + skillCooldownSeconds;
            NextBowShotAllowedAt = simTime + bowFireCooldownSeconds;

            FireBowProjectile(SelectedBowProjectileType, true);

            if (bowAutoExitAimOnShoot)
            {
                IsAiming = false;
                BowRequireAimRelease = true;
            }
            else
            {
                IsAiming = aimHeld;
                BowRequireAimRelease = false;
            }
        }
        else if (fireRequested && canFireNow)
        {
            AttackPressed = true;
            AttackSequence++;
            NetCombatState = CombatState.Attacking;
            NextBowShotAllowedAt = simTime + bowFireCooldownSeconds;

            FireBowProjectile(SelectedBowProjectileType, false);

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

    private void FireBowProjectile(BowWeapon.ProjectileType projectileType, bool spawnSkillEffect)
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

        Vector3 aimPoint = BowWeapon.GetAimPointFromCamera(transform, bowAimRayDistance, bowAimLayerMask);
        RPC_SpawnBowProjectile(aimPoint, attackerRef, projectileType, spawnSkillEffect);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SpawnBowProjectile(Vector3 aimPoint, PlayerRef attackerRef, BowWeapon.ProjectileType projectileType, bool spawnSkillEffect)
    {
        if (bowWeapon == null)
        {
            bowWeapon = GetComponentInChildren<BowWeapon>();
        }

        if (bowWeapon == null)
        {
            return;
        }

        if (spawnSkillEffect)
        {
            bowWeapon.FireSkillShot(transform, attackerRef, aimPoint, HasStateAuthority);
            return;
        }

        bowWeapon.SpawnProjectile(transform, attackerRef, aimPoint, HasStateAuthority, projectileType, false);
    }

    private bool IsFrozenAt(float simTime)
    {
        return simTime < FrozenUntil;
    }

    private void ApplyFreeze(float duration)
    {
        float simTime = Runner != null ? (float)Runner.SimulationTime : Time.time;
        FrozenUntil = Mathf.Max(FrozenUntil, simTime + Mathf.Max(0f, duration));
        FreezeSequence++;
        NetCombatState = CombatState.None;
        IsBlocking = false;
        IsAiming = false;
        AttackPressed = false;
        UseSkillSword = false;
        SwordSkillUntil = 0f;
    }

    private Vector3 GetAimForward()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            return transform.forward;
        }

        // Get aim direction from screen center for consistent rotatio
        Ray screenCenterRay = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 forward = screenCenterRay.direction;
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.0001f)
        {
            return transform.forward;
        }

        return forward.normalized;
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
