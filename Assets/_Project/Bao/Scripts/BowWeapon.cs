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

    public bool Fire(Transform ownerRoot, PlayerRef attackerRef, Vector3 aimDirection)
    {
        if (projectilePrefab == null)
        {
            return false;
        }

        Transform spawn = projectileSpawnPoint != null ? projectileSpawnPoint : transform;
        Vector3 direction = aimDirection.sqrMagnitude > 0.0001f ? aimDirection.normalized : spawn.forward;
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
        BowProjectile projectile = Instantiate(projectilePrefab, spawn.position, rotation);
        projectile.Initialize(ownerRoot, attackerRef, damage, projectileSpeed, projectileLifetime);
        return true;
    }
}
