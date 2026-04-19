using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
public class MageFireProjectile : MonoBehaviour
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
    private float burnDuration;
    private float burnTickDamage;
    private float burnTickInterval;
    private float burnDecayFactor;

    private void Awake()
    {
        if (projectileBody == null)
        {
            projectileBody = GetComponent<Rigidbody>();
        }
    }

    public void Initialize(
        Transform owner,
        PlayerRef attacker,
        int damageAmount,
        float speed,
        float lifetime,
        bool canDealDamage,
        float burnDuration,
        float burnTickDamage,
        float burnTickInterval,
        float burnDecayFactor)
    {
        ownerRoot = owner;
        attackerRef = attacker;
        damage = Mathf.Max(1, damageAmount);
        lifeTimer = Mathf.Max(0.1f, lifetime);
        this.canDealDamage = canDealDamage;
        this.burnDuration = Mathf.Max(0f, burnDuration);
        this.burnTickDamage = Mathf.Max(0f, burnTickDamage);
        this.burnTickInterval = Mathf.Max(0.05f, burnTickInterval);
        this.burnDecayFactor = Mathf.Clamp(burnDecayFactor, 0f, 1f);
        IgnoreOwnerCollisions();
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

        if (ownerRoot != null && (other.transform.root == ownerRoot || other.transform.IsChildOf(ownerRoot)))
        {
            return;
        }

        PlayerHealth targetHealth = other.GetComponentInParent<PlayerHealth>();
        SharedModePlayerController networkTarget = other.GetComponentInParent<SharedModePlayerController>();
        EnemyHealth enemyTarget = other.GetComponentInParent<EnemyHealth>();

        bool hasDamageTarget = targetHealth != null || networkTarget != null || enemyTarget != null;
        if (hasDamageTarget && canDealDamage)
        {
            bool targetIsDead = (networkTarget != null && networkTarget.IsDead) ||
                                (targetHealth != null && targetHealth.IsDead) ||
                                (enemyTarget != null && enemyTarget.IsDead);

            if (!targetIsDead)
            {
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

                    networkTarget.RequestBurn(burnDuration, burnTickDamage, burnTickInterval, burnDecayFactor, attackerRef);
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

                    enemyTarget.RequestBurn(burnDuration, burnTickDamage, burnTickInterval, burnDecayFactor, attackerRef);
                }
                else
                {
                    targetHealth.TakeDamageFromOrigin(damage, transform.position);
                    targetHealth.ApplyBurn(burnDuration, burnTickDamage, burnTickInterval, burnDecayFactor);
                }

                if (logHit)
                {
                    string targetName = networkTarget != null
                        ? networkTarget.name
                        : enemyTarget != null
                            ? enemyTarget.name
                            : targetHealth.name;
                    Debug.Log($"MageFireProjectile hit {targetName} for {damage}.");
                }
            }
        }

        hasHit = true;
        if (destroyOnHit)
        {
            Destroy(gameObject);
        }
    }

    private void IgnoreOwnerCollisions()
    {
        if (ownerRoot == null)
        {
            return;
        }

        Collider[] ownerColliders = ownerRoot.GetComponentsInChildren<Collider>(true);
        if (ownerColliders == null || ownerColliders.Length == 0)
        {
            return;
        }

        Collider[] projectileColliders = GetComponentsInChildren<Collider>(true);
        if (projectileColliders == null || projectileColliders.Length == 0)
        {
            return;
        }

        for (int i = 0; i < projectileColliders.Length; i++)
        {
            Collider projectileCollider = projectileColliders[i];
            if (projectileCollider == null)
            {
                continue;
            }

            for (int j = 0; j < ownerColliders.Length; j++)
            {
                Collider ownerCollider = ownerColliders[j];
                if (ownerCollider == null)
                {
                    continue;
                }

                Physics.IgnoreCollision(projectileCollider, ownerCollider, true);
            }
        }
    }
}
