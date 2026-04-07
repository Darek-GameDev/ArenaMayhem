using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
public class BowWeapon : MonoBehaviour
{
    [SerializeField] private BowProjectile projectilePrefab;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private float projectileSpeed = 28f;
    [SerializeField] private float projectileLifetime = 4f;
    [SerializeField] private int damage = 1;

    public bool Fire(Transform ownerRoot, PlayerRef attackerRef)
    {
        if (projectilePrefab == null)
        {
            return false;
        }

        Transform spawn = projectileSpawnPoint != null ? projectileSpawnPoint : transform;
        BowProjectile projectile = Instantiate(projectilePrefab, spawn.position, spawn.rotation);
        projectile.Initialize(ownerRoot, attackerRef, damage, projectileSpeed, projectileLifetime);
        return true;
    }
}
