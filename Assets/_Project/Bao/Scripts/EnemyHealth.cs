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

    [Networked] public int NetHealth { get; set; }
    [Networked] public NetworkBool NetIsDead { get; set; }
    [Networked] public int HitSequence { get; set; }
    [Networked] public float FrozenUntil { get; set; }
    [Networked] public int FreezeSequence { get; set; }

    private int localHealth;
    private bool localIsDead;
    private bool localInitialized;
    private bool deadTriggered;
    private int lastRenderedHitSequence = -1;
    private int lastRenderedFreezeSequence = -1;
    private bool lastRenderedFrozen;
    private GameObject freezeVfxInstance;
    private float localFrozenUntil;

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
        }

        lastRenderedHitSequence = HitSequence;
        lastRenderedFreezeSequence = FreezeSequence;
        lastRenderedFrozen = IsFrozen(Runner != null ? (float)Runner.SimulationTime : Time.time);
        SyncAnimatorState();
        RefreshFreezeVisuals(lastRenderedFrozen);
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

        SyncAnimatorState();
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

    private void EnsureLocalInitialized()
    {
        if (localInitialized)
        {
            return;
        }

        localHealth = maxHealth;
        localIsDead = false;
        localFrozenUntil = 0f;
        localInitialized = true;
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
}
