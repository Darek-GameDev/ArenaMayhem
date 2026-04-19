using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyHealth : NetworkBehaviour
{
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private bool logDamage = false;
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject freezeVfxPrefab;
    [SerializeField] private Transform freezeVfxAnchor;
    [SerializeField] private GameObject burnVfxPrefab;
    [SerializeField] private Transform burnVfxAnchor;

    [Networked] public int NetHealth { get; set; }
    [Networked] public NetworkBool NetIsDead { get; set; }
    [Networked] public int HitSequence { get; set; }
    [Networked] public float FrozenUntil { get; set; }
    [Networked] public int FreezeSequence { get; set; }
    [Networked] public float BurnUntil { get; set; }
    [Networked] public float BurnNextTickAt { get; set; }
    [Networked] public float BurnTickInterval { get; set; }
    [Networked] public float BurnTickDamage { get; set; }
    [Networked] public float BurnDecayFactor { get; set; }
    [Networked] public PlayerRef BurnAttackerRef { get; set; }

    private int localHealth;
    private bool localIsDead;
    private bool localInitialized;
    private bool deadTriggered;
    private int lastRenderedHitSequence = -1;
    private int lastRenderedFreezeSequence = -1;
    private bool lastRenderedFrozen;
    private bool lastRenderedBurned;
    private GameObject freezeVfxInstance;
    private GameObject burnVfxInstance;
    private float localFrozenUntil;
    private float localBurnedUntil;
    private float localBurnNextTickAt;
    private float localBurnTickInterval;
    private float localBurnTickDamage;
    private float localBurnDecayFactor = 1f;

    public int Health => HasNetworkState ? NetHealth : localHealth;
    public bool IsDead => HasNetworkState ? NetIsDead : localIsDead;
    public int MaxHealth => maxHealth;
    public bool IsFrozen(float simTime) => HasNetworkState ? simTime < FrozenUntil : Time.time < localFrozenUntil;

    private bool HasNetworkState => Object != null && Object.IsValid;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        EnsureWorldSpaceHealthBar();

        EnsureLocalInitialized();
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            NetHealth = maxHealth;
            NetIsDead = false;
            HitSequence = 0;
            FrozenUntil = 0f;
            FreezeSequence = 0;
            BurnUntil = 0f;
            BurnNextTickAt = 0f;
            BurnTickInterval = 0f;
            BurnTickDamage = 0f;
            BurnDecayFactor = 1f;
            BurnAttackerRef = default;
        }

        lastRenderedHitSequence = HitSequence;
        lastRenderedFreezeSequence = FreezeSequence;
        lastRenderedFrozen = IsFrozen(Runner != null ? (float)Runner.SimulationTime : Time.time);
        lastRenderedBurned = IsBurnActive(Runner != null ? (float)Runner.SimulationTime : Time.time);
        SyncAnimatorState();
        RefreshFreezeVisuals(lastRenderedFrozen);
        RefreshBurnVisuals(lastRenderedBurned);
    }

    public override void Render()
    {
        if (!HasNetworkState)
        {
            return;
        }

        if (lastRenderedHitSequence != HitSequence)
        {
            if (lastRenderedHitSequence >= 0 && !NetIsDead)
            {
                animator?.SetTrigger("GetHit");
            }

            lastRenderedHitSequence = HitSequence;
        }

        bool frozen = IsFrozen((float)Runner.SimulationTime);
        if (frozen != lastRenderedFrozen || lastRenderedFreezeSequence != FreezeSequence)
        {
            lastRenderedFrozen = frozen;
            lastRenderedFreezeSequence = FreezeSequence;
            RefreshFreezeVisuals(frozen);
        }

        bool burned = IsBurnActive((float)Runner.SimulationTime);
        if (burned != lastRenderedBurned)
        {
            lastRenderedBurned = burned;
            RefreshBurnVisuals(burned);
        }

        SyncAnimatorState();
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
        {
            return;
        }

        ProcessNetworkBurn((float)Runner.SimulationTime);
    }

    private void Update()
    {
        if (!HasNetworkState)
        {
            SyncAnimatorState();
            bool frozen = Time.time < localFrozenUntil;
            if (frozen != lastRenderedFrozen)
            {
                lastRenderedFrozen = frozen;
                RefreshFreezeVisuals(frozen);
            }

            bool burned = IsBurnActive(Time.time);
            if (burned != lastRenderedBurned)
            {
                lastRenderedBurned = burned;
                RefreshBurnVisuals(burned);
            }

            ProcessLocalBurn();
        }
    }

    public void RequestDamage(int amount)
    {
        RequestDamageInternal(amount, default);
    }

    public void RequestDamageFromPlayer(int amount, PlayerRef attackerRef)
    {
        RequestDamageInternal(amount, attackerRef);
    }

    public void RequestFreeze(float duration)
    {
        if (duration <= 0f || IsDead)
        {
            return;
        }

        if (HasNetworkState)
        {
            if (HasStateAuthority)
            {
                ApplyFreeze(duration);
            }
            else
            {
                RPC_RequestFreeze(duration);
            }

            return;
        }

        ApplyLocalFreeze(duration);
    }

    public void RequestBurn(float duration, float tickDamage, float tickInterval, float decayFactor)
    {
        RequestBurn(duration, tickDamage, tickInterval, decayFactor, default);
    }

    public void RequestBurn(float duration, float tickDamage, float tickInterval, float decayFactor, PlayerRef attackerRef)
    {
        if (duration <= 0f || tickDamage <= 0f || tickInterval <= 0f || IsDead)
        {
            return;
        }

        if (HasNetworkState)
        {
            if (HasStateAuthority)
            {
                ApplyBurn(duration, tickDamage, tickInterval, decayFactor, attackerRef);
            }
            else
            {
                RPC_RequestBurn(duration, tickDamage, tickInterval, decayFactor, attackerRef);
            }

            return;
        }

        ApplyLocalBurn(duration, tickDamage, tickInterval, decayFactor);
    }

    private void RequestDamageInternal(int amount, PlayerRef attackerRef)
    {
        if (amount <= 0 || IsDead)
        {
            return;
        }

        if (HasNetworkState)
        {
            if (attackerRef.IsRealPlayer)
            {
                RPC_RequestDamageFromPlayer(amount, attackerRef);
            }
            else
            {
                RPC_RequestDamage(amount);
            }
            return;
        }

        ApplyLocalDamage(amount);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestDamage(int amount)
    {
        if (amount <= 0 || NetIsDead)
        {
            return;
        }

        HitSequence++;
        NetHealth = Mathf.Max(0, NetHealth - amount);

        if (logDamage)
        {
            Debug.Log($"{name} took {amount} damage. HP: {NetHealth}/{maxHealth}");
        }

        if (NetHealth == 0)
        {
            NetIsDead = true;
            if (logDamage)
            {
                Debug.Log($"{name} is dead.");
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestDamageFromPlayer(int amount, PlayerRef attackerRef)
    {
        if (amount <= 0 || NetIsDead)
        {
            return;
        }

        HitSequence++;
        NetHealth = Mathf.Max(0, NetHealth - amount);

        if (logDamage)
        {
            Debug.Log($"{name} took {amount} damage. HP: {NetHealth}/{maxHealth}");
        }

        if (NetHealth == 0)
        {
            NetIsDead = true;

            SharedModePlayerController attacker = ResolvePlayerController(attackerRef);
            if (attacker != null)
            {
                attacker.RPC_RequestAddKill(1);
            }

            if (logDamage)
            {
                Debug.Log($"{name} is dead.");
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestFreeze(float duration)
    {
        if (duration <= 0f || NetIsDead)
        {
            return;
        }

        ApplyFreeze(duration);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestBurn(float duration, float tickDamage, float tickInterval, float decayFactor, PlayerRef attackerRef)
    {
        if (duration <= 0f || tickDamage <= 0f || tickInterval <= 0f || NetIsDead)
        {
            return;
        }

        ApplyBurn(duration, tickDamage, tickInterval, decayFactor, attackerRef);
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

    private void ApplyLocalDamage(int amount)
    {
        EnsureLocalInitialized();

        if (localIsDead)
        {
            return;
        }

        localHealth = Mathf.Max(0, localHealth - amount);
        animator?.SetTrigger("GetHit");

        if (logDamage)
        {
            Debug.Log($"{name} took {amount} damage. HP: {localHealth}/{maxHealth}");
        }

        if (localHealth == 0)
        {
            localIsDead = true;
            if (logDamage)
            {
                Debug.Log($"{name} is dead.");
            }
        }
    }

    private void ApplyFreeze(float duration)
    {
        float simTime = Runner != null ? (float)Runner.SimulationTime : Time.time;
        FrozenUntil = Mathf.Max(FrozenUntil, simTime + Mathf.Max(0f, duration));
        FreezeSequence++;
    }

    private void ApplyBurn(float duration, float tickDamage, float tickInterval, float decayFactor, PlayerRef attackerRef)
    {
        float simTime = Runner != null ? (float)Runner.SimulationTime : Time.time;
        BurnUntil = simTime + Mathf.Max(0f, duration);
        BurnTickInterval = Mathf.Max(0.05f, tickInterval);
        BurnTickDamage = Mathf.Max(0.1f, tickDamage);
        BurnDecayFactor = Mathf.Clamp(decayFactor, 0f, 1f);
        BurnNextTickAt = simTime + BurnTickInterval;
        BurnAttackerRef = attackerRef;
    }

    private void ApplyLocalFreeze(float duration)
    {
        EnsureLocalInitialized();

        if (localIsDead)
        {
            return;
        }

        localFrozenUntil = Mathf.Max(localFrozenUntil, Time.time + Mathf.Max(0f, duration));
        FreezeSequence++;
    }

    private void ApplyLocalBurn(float duration, float tickDamage, float tickInterval, float decayFactor)
    {
        EnsureLocalInitialized();

        if (localIsDead)
        {
            return;
        }

        localBurnedUntil = Time.time + Mathf.Max(0f, duration);
        localBurnTickInterval = Mathf.Max(0.05f, tickInterval);
        localBurnTickDamage = Mathf.Max(0.1f, tickDamage);
        localBurnDecayFactor = Mathf.Clamp(decayFactor, 0f, 1f);
        localBurnNextTickAt = Time.time + localBurnTickInterval;
    }

    private void EnsureLocalInitialized()
    {
        if (localInitialized)
        {
            return;
        }

        localHealth = maxHealth;
        localIsDead = false;
        localFrozenUntil = 0f;
        localBurnedUntil = 0f;
        localBurnNextTickAt = 0f;
        localBurnTickInterval = 0f;
        localBurnTickDamage = 0f;
        localBurnDecayFactor = 1f;
        localInitialized = true;
    }

    private void ProcessNetworkBurn(float simTime)
    {
        if (NetIsDead || BurnUntil <= simTime || BurnTickInterval <= 0f || BurnTickDamage <= 0f)
        {
            return;
        }

        if (BurnNextTickAt > simTime)
        {
            return;
        }

        int tickDamage = Mathf.Max(1, Mathf.RoundToInt(BurnTickDamage));
        HitSequence++;
        NetHealth = Mathf.Max(0, NetHealth - tickDamage);

        if (NetHealth == 0)
        {
            NetIsDead = true;

            if (BurnAttackerRef.IsRealPlayer)
            {
                SharedModePlayerController attacker = ResolvePlayerController(BurnAttackerRef);
                if (attacker != null)
                {
                    attacker.RPC_RequestAddKill(1);
                }
            }
        }

        BurnTickDamage = Mathf.Max(0f, BurnTickDamage * BurnDecayFactor);
        BurnNextTickAt = simTime + BurnTickInterval;
    }

    private void ProcessLocalBurn()
    {
        if (localIsDead || localBurnedUntil <= Time.time || localBurnTickInterval <= 0f || localBurnTickDamage <= 0f)
        {
            return;
        }

        if (localBurnNextTickAt > Time.time)
        {
            return;
        }

        int tickDamage = Mathf.Max(1, Mathf.RoundToInt(localBurnTickDamage));
        ApplyLocalDamage(tickDamage);
        localBurnTickDamage = Mathf.Max(0f, localBurnTickDamage * localBurnDecayFactor);
        localBurnNextTickAt = Time.time + localBurnTickInterval;
    }

    private void SyncAnimatorState()
    {
        if (animator == null)
        {
            return;
        }

        bool isDead = IsDead;
        animator.SetBool("isDead", isDead);

        if (isDead)
        {
            if (!deadTriggered)
            {
                animator.SetTrigger("DeadTrigger");
                deadTriggered = true;
            }
        }
        else
        {
            deadTriggered = false;
        }
    }

    private void EnsureWorldSpaceHealthBar()
    {
        WorldSpaceHealthBar healthBar = GetComponent<WorldSpaceHealthBar>();
        if (healthBar == null)
        {
            healthBar = gameObject.AddComponent<WorldSpaceHealthBar>();
        }

        healthBar.ConfigureForEnemy(this);
    }

    private void RefreshFreezeVisuals(bool frozen)
    {
        if (frozen)
        {
            if (freezeVfxPrefab == null)
            {
                return;
            }

            if (freezeVfxInstance != null)
            {
                Destroy(freezeVfxInstance);
            }

            Transform parent = freezeVfxAnchor != null ? freezeVfxAnchor : transform;
            freezeVfxInstance = Instantiate(freezeVfxPrefab, parent.position, parent.rotation, parent);
        }
        else if (freezeVfxInstance != null)
        {
            Destroy(freezeVfxInstance);
            freezeVfxInstance = null;
        }
    }

    private bool IsBurnActive(float simTime)
    {
        return !IsDead && ((HasNetworkState && simTime < BurnUntil) || (!HasNetworkState && Time.time < localBurnedUntil));
    }

    private void RefreshBurnVisuals(bool burned)
    {
        if (burned)
        {
            if (burnVfxPrefab == null)
            {
                return;
            }

            if (burnVfxInstance != null)
            {
                Destroy(burnVfxInstance);
            }

            Transform parent = burnVfxAnchor != null ? burnVfxAnchor : transform;
            burnVfxInstance = Instantiate(burnVfxPrefab, parent.position, parent.rotation, parent);
        }
        else if (burnVfxInstance != null)
        {
            Destroy(burnVfxInstance);
            burnVfxInstance = null;
        }
    }

    private void OnDisable()
    {
        if (burnVfxInstance != null)
        {
            Destroy(burnVfxInstance);
            burnVfxInstance = null;
        }
    }
}
