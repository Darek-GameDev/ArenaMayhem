using System.Collections.Generic;
using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
public class MageAoeFireZone : MonoBehaviour
{
    [SerializeField] private SphereCollider triggerCollider;
    [SerializeField] private bool logDamage;

    private readonly Dictionary<int, float> nextDamageAtByTarget = new Dictionary<int, float>();

    private Transform ownerRoot;
    private PlayerRef attackerRef;
    private int damagePerTick;
    private float damageTickInterval;
    private float burnDuration;
    private float burnTickDamage;
    private float burnTickInterval;
    private float burnDecayFactor;
    private float lifeTimer;
    private bool canDealDamage;
    private bool initialized;

    private void Awake()
    {
        if (triggerCollider == null)
        {
            triggerCollider = GetComponent<SphereCollider>();
        }

        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<SphereCollider>();
        }

        triggerCollider.isTrigger = true;
    }

    public void Initialize(
        Transform owner,
        PlayerRef attacker,
        int damagePerTick,
        float damageTickInterval,
        float lifetime,
        bool canDealDamage,
        float burnDuration,
        float burnTickDamage,
        float burnTickInterval,
        float burnDecayFactor,
        float radius)
    {
        ownerRoot = owner;
        attackerRef = attacker;
        this.damagePerTick = Mathf.Max(1, damagePerTick);
        this.damageTickInterval = Mathf.Max(0.05f, damageTickInterval);
        lifeTimer = Mathf.Max(0.1f, lifetime);
        this.canDealDamage = canDealDamage;
        this.burnDuration = Mathf.Max(0f, burnDuration);
        this.burnTickDamage = Mathf.Max(0f, burnTickDamage);
        this.burnTickInterval = Mathf.Max(0.05f, burnTickInterval);
        this.burnDecayFactor = Mathf.Clamp(burnDecayFactor, 0f, 1f);

        if (triggerCollider != null)
        {
            triggerCollider.radius = Mathf.Max(0.1f, radius);
            triggerCollider.isTrigger = true;
        }

        initialized = true;
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
        TryAffectTarget(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryAffectTarget(other);
    }

    private void TryAffectTarget(Collider other)
    {
        if (!initialized || !canDealDamage || other == null)
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

        if (targetHealth == null && networkTarget == null && enemyTarget == null)
        {
            return;
        }

        bool targetIsDead = (networkTarget != null && networkTarget.IsDead) ||
                            (targetHealth != null && targetHealth.IsDead) ||
                            (enemyTarget != null && enemyTarget.IsDead);
        if (targetIsDead)
        {
            return;
        }

        int key = networkTarget != null
            ? networkTarget.GetInstanceID()
            : enemyTarget != null
                ? enemyTarget.GetInstanceID()
                : targetHealth.GetInstanceID();

        float now = Time.time;
        if (nextDamageAtByTarget.TryGetValue(key, out float nextDamageAt) && now < nextDamageAt)
        {
            return;
        }

        nextDamageAtByTarget[key] = now + damageTickInterval;

        if (networkTarget != null)
        {
            if (attackerRef.IsRealPlayer)
            {
                networkTarget.RPC_RequestEnvironmentalDamageFromPlayer(damagePerTick, attackerRef);
            }
            else
            {
                networkTarget.RPC_RequestEnvironmentalDamage(damagePerTick);
            }

            networkTarget.RequestBurn(burnDuration, burnTickDamage, burnTickInterval, burnDecayFactor, attackerRef);
        }
        else if (enemyTarget != null)
        {
            if (attackerRef.IsRealPlayer)
            {
                enemyTarget.RequestDamageFromPlayer(damagePerTick, attackerRef);
            }
            else
            {
                enemyTarget.RequestDamage(damagePerTick);
            }

            enemyTarget.RequestBurn(burnDuration, burnTickDamage, burnTickInterval, burnDecayFactor, attackerRef);
        }
        else
        {
            targetHealth.TakeEnvironmentalDamage(damagePerTick);
            targetHealth.ApplyBurn(burnDuration, burnTickDamage, burnTickInterval, burnDecayFactor);
        }

        if (logDamage)
        {
            string targetName = networkTarget != null
                ? networkTarget.name
                : enemyTarget != null
                    ? enemyTarget.name
                    : targetHealth.name;
            Debug.Log($"MageAoeFireZone damaged {targetName} for {damagePerTick}.");
        }
    }
}
