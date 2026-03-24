using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damage = 1;
    [SerializeField] private Collider hitbox;
    [SerializeField] private Transform ownerRoot;
    [SerializeField] private bool logHits = false;

    private readonly HashSet<PlayerHealth> hitTargets = new HashSet<PlayerHealth>();
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
        if (targetHealth == null || targetHealth.IsDead)
        {
            return;
        }

        if (hitTargets.Contains(targetHealth))
        {
            return;
        }

        hitTargets.Add(targetHealth);
        targetHealth.TakeDamage(damage);

        if (logHits)
        {
            Debug.Log($"{name} hit {targetHealth.name} for {damage}.");
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
