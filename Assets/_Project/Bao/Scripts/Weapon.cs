using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damage = 1;
    [SerializeField] private Collider hitbox;
    [SerializeField] private Transform ownerRoot;
    [SerializeField] private bool logHits = false;

    private readonly HashSet<PlayerHealth> hitTargets = new HashSet<PlayerHealth>();
    private readonly HashSet<SharedModePlayerController> hitNetworkTargets = new HashSet<SharedModePlayerController>();
    private readonly HashSet<EnemyHealth> hitEnemyTargets = new HashSet<EnemyHealth>();
    private bool attackWindowOpen;

    private void Awake()
    {
        if (hitbox == null)
        {
            hitbox = GetComponent<Collider>();
        }

        if (ownerRoot == null)
        {
            ownerRoot = transform.root;
        }

        SetHitboxActive(false);
    }

    public void SetOwner(Transform owner)
    {
        ownerRoot = owner;
    }

    // Call this from animation event when the sword can deal damage.
    public void BeginAttackWindow()
    {
        attackWindowOpen = true;
        hitTargets.Clear();
        hitNetworkTargets.Clear();
        hitEnemyTargets.Clear();
        SetHitboxActive(true);
    }

    // Call this from animation event when the swing ends.
    public void EndAttackWindow()
    {
        attackWindowOpen = false;
        SetHitboxActive(false);
    }

    private void OnDisable()
    {
        EndAttackWindow();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryDealDamage(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!attackWindowOpen)
        {
            return;
        }

        // Ensures already-overlapping targets can still be hit.
        TryDealDamage(other);
    }

    private void TryDealDamage(Collider other)
    {
        if (!attackWindowOpen)
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

        bool targetIsDead = (networkTarget != null && networkTarget.IsDead) ||
                            (targetHealth != null && targetHealth.IsDead) ||
                            (enemyTarget != null && enemyTarget.IsDead);
        if (targetIsDead)
        {
            return;
        }

        if (targetHealth == null && networkTarget == null && enemyTarget == null)
        {
            return;
        }

        // For legacy health flow we keep the existing dedupe map.
        if (targetHealth != null && hitTargets.Contains(targetHealth))
        {
            return;
        }

        if (networkTarget != null && hitNetworkTargets.Contains(networkTarget))
        {
            return;
        }

        if (enemyTarget != null && hitEnemyTargets.Contains(enemyTarget))
        {
            return;
        }

        if (targetHealth != null)
        {
            hitTargets.Add(targetHealth);
        }

        if (networkTarget != null)
        {
            hitNetworkTargets.Add(networkTarget);
        }

        if (enemyTarget != null)
        {
            hitEnemyTargets.Add(enemyTarget);
        }

        SharedModePlayerController ownerController = ownerRoot != null
            ? ownerRoot.GetComponentInParent<SharedModePlayerController>()
            : null;
        PlayerRef attackerRef = default;
        if (ownerController != null && ownerController.Object != null && ownerController.Object.IsValid)
        {
            attackerRef = ownerController.Object.InputAuthority;
        }

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

        if (logHits)
        {
            string targetName = networkTarget != null
                ? networkTarget.name
                : enemyTarget != null
                    ? enemyTarget.name
                    : targetHealth.name;
            Debug.Log($"{name} hit {targetName} for {damage}.");
        }
    }

    private void SetHitboxActive(bool enabled)
    {
        if (hitbox != null)
        {
            hitbox.enabled = enabled;
        }
    }
}
