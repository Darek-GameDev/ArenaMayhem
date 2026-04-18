using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
public class BowProjectile : MonoBehaviour
{
    [SerializeField] private Rigidbody projectileBody;
    [SerializeField] private bool destroyOnHit = true;
    [SerializeField] private bool logHit;

    private Transform ownerRoot;
    private PlayerRef attackerRef;
    private int damage;
    private float lifeTimer;
    private bool initialized;
    private bool hasHit;
    private bool canDealDamage = true;
    private bool applyFreezeOnHit;
    private float freezeDuration;

    private void Awake()
    {
        if (projectileBody == null)
        {
            projectileBody = GetComponent<Rigidbody>();
        }
    }

    public void Initialize(Transform owner, PlayerRef attacker, int damageAmount, float speed, float lifetime, bool canDealDamage)
    {
        Initialize(owner, attacker, damageAmount, speed, lifetime, canDealDamage, false, 0f);
    }

    public void Initialize(Transform owner, PlayerRef attacker, int damageAmount, float speed, float lifetime, bool canDealDamage, bool applyFreezeOnHit, float freezeDuration)
    {
        ownerRoot = owner;
        attackerRef = attacker;
        damage = Mathf.Max(1, damageAmount);
        lifeTimer = Mathf.Max(0.1f, lifetime);
        this.canDealDamage = canDealDamage;
        this.applyFreezeOnHit = applyFreezeOnHit;
        this.freezeDuration = Mathf.Max(0f, freezeDuration);
        initialized = true;

        if (projectileBody != null)
        {
            projectileBody.linearVelocity = transform.forward * Mathf.Max(0.1f, speed);
        }
    }

    private void Update()
    {
        if (!initialized)
        {
            return;
        }

        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryDamageTarget(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null)
        {
            return;
        }

        TryDamageTarget(collision.collider);
    }

    private void TryDamageTarget(Collider other)
    {
        if (!initialized || hasHit || other == null)
        {
            return;
        }

        if (ownerRoot != null && other.transform.root == ownerRoot)
        {
            return;
        }

        PlayerHealth targetHealth = other.GetComponentInParent<PlayerHealth>();
        SharedModePlayerController networkTarget = other.GetComponentInParent<SharedModePlayerController>();
        EnemyHealth enemyTarget = other.GetComponentInParent<EnemyHealth>();

        bool hasDamageTarget = targetHealth != null || networkTarget != null || enemyTarget != null;
        if (hasDamageTarget)
        {
            if (canDealDamage)
            {
                bool targetIsDead = (networkTarget != null && networkTarget.IsDead) ||
                                    (targetHealth != null && targetHealth.IsDead) ||
                                    (enemyTarget != null && enemyTarget.IsDead);
                if (!targetIsDead)
                {
                    if (applyFreezeOnHit && freezeDuration > 0f)
                    {
                        if (networkTarget != null)
                        {
                            networkTarget.RequestFreeze(freezeDuration);
                        }
                        else if (enemyTarget != null)
                        {
                            enemyTarget.RequestFreeze(freezeDuration);
                        }
                        else if (targetHealth != null)
                        {
                            targetHealth.ApplyFreeze(freezeDuration);
                        }
                    }

                    if (networkTarget != null)
                    {
                        Vector3 hitOrigin = transform.position;
                        if (attackerRef.IsRealPlayer)
                        {
                            networkTarget.RPC_RequestDamageFromPlayerWithOrigin(damage, attackerRef, hitOrigin);
                        }
                        else
                        {
                            networkTarget.RPC_RequestDamageWithOrigin(damage, hitOrigin);
                        }
                    }
                    else if (enemyTarget != null)
                    {
                        if (attackerRef.IsRealPlayer)
                        {
                            enemyTarget.RequestDamageFromPlayer(damage, attackerRef);
                        }
                        else
                        {
                            enemyTarget.RequestDamage(damage);
                        }
                    }
                    else
                    {
                        targetHealth.TakeDamageFromOrigin(damage, transform.position);
                    }

                    if (logHit)
                    {
                        string targetName = networkTarget != null
                            ? networkTarget.name
                            : enemyTarget != null
                                ? enemyTarget.name
                                : targetHealth.name;
                        Debug.Log($"BowProjectile hit {targetName} for {damage}.");
                    }
                }
            }
        }

        hasHit = true;
        if (destroyOnHit)
        {
            Destroy(gameObject);
        }
    }
}
