using System.Collections.Generic;
using UnityEngine;

public class EnemyWeapon : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damage = 1;
    [SerializeField] private Collider hitbox;
    [SerializeField] private Transform ownerRoot;
    [SerializeField] private bool logHits = false;

    private readonly HashSet<PlayerHealth> hitTargets = new HashSet<PlayerHealth>();
    private readonly HashSet<SharedModePlayerController> hitNetworkTargets = new HashSet<SharedModePlayerController>();
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

    public void BeginAttackWindow()
    {
        attackWindowOpen = true;
        hitTargets.Clear();
        hitNetworkTargets.Clear();
        SetHitboxActive(true);
    }

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

        bool targetIsDead = (networkTarget != null && networkTarget.IsDead) ||
                            (targetHealth != null && targetHealth.IsDead);
        if (targetIsDead)
        {
            return;
        }

        if (targetHealth == null && networkTarget == null)
        {
            return;
        }

        if (targetHealth != null && hitTargets.Contains(targetHealth))
        {
            return;
        }

        if (networkTarget != null && hitNetworkTargets.Contains(networkTarget))
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

        if (networkTarget != null)
        {
            Vector3 hitOrigin = ownerRoot != null ? ownerRoot.position : transform.position;
            networkTarget.RPC_RequestDamageWithOrigin(damage, hitOrigin);
        }
        else
        {
            Vector3 hitOrigin = ownerRoot != null ? ownerRoot.position : transform.position;
            targetHealth.TakeDamageFromOrigin(damage, hitOrigin);
        }

        if (logHits)
        {
            string targetName = networkTarget != null ? networkTarget.name : targetHealth.name;
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
