using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyHealth : NetworkBehaviour
{
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private bool logDamage = false;
    [SerializeField] private Animator animator;

    [Networked] public int NetHealth { get; set; }
    [Networked] public NetworkBool NetIsDead { get; set; }
    [Networked] public int HitSequence { get; set; }

    private int localHealth;
    private bool localIsDead;
    private bool localInitialized;
    private bool deadTriggered;
    private int lastRenderedHitSequence = -1;

    public int Health => HasNetworkState ? NetHealth : localHealth;
    public bool IsDead => HasNetworkState ? NetIsDead : localIsDead;
    public int MaxHealth => maxHealth;

    private bool HasNetworkState => Object != null && Object.IsValid;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        EnsureLocalInitialized();
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            NetHealth = maxHealth;
            NetIsDead = false;
            HitSequence = 0;
        }

        lastRenderedHitSequence = HitSequence;
        SyncAnimatorState();
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

        SyncAnimatorState();
    }

    private void Update()
    {
        if (!HasNetworkState)
        {
            SyncAnimatorState();
        }
    }

    public void RequestDamage(int amount)
    {
        if (amount <= 0 || IsDead)
        {
            return;
        }

        if (HasNetworkState)
        {
            RPC_RequestDamage(amount);
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

    private void EnsureLocalInitialized()
    {
        if (localInitialized)
        {
            return;
        }

        localHealth = maxHealth;
        localIsDead = false;
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
}
