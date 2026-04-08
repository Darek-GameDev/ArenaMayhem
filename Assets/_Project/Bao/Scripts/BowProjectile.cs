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

    private void Awake()
    {
        if (projectileBody == null)
        {
            projectileBody = GetComponent<Rigidbody>();
        }
    }

    public void Initialize(Transform owner, PlayerRef attacker, int damageAmount, float speed, float lifetime, bool canDealDamage)
    {
        ownerRoot = owner;
        attackerRef = attacker;
        damage = Mathf.Max(1, damageAmount);
        lifeTimer = Mathf.Max(0.1f, lifetime);
        this.canDealDamage = canDealDamage;
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
                    if (networkTarget != null)
                    {
                        if (attackerRef.IsRealPlayer)
                        {
                            networkTarget.RPC_RequestDamageFromPlayer(damage, attackerRef);
                        }
                        else
                        {
                            networkTarget.RPC_RequestDamage(damage);
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
                        targetHealth.TakeDamage(damage);
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
