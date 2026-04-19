using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
public class MageWeapon : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField] private MageFireProjectile projectilePrefab;
    [SerializeField] private float projectileSpeed = 28f;
    [SerializeField] private float projectileLifetime = 4f;
    [SerializeField] private int projectileDamage = 1;

    [Header("AOE")]
    [SerializeField] private GameObject aoeZonePrefab;
    [SerializeField] private float aoeLifetime = 3f;
    [SerializeField] private float aoeRadius = 3.5f;
    [SerializeField] private int aoeDamagePerTick = 1;
    [SerializeField] private float aoeDamageTickInterval = 0.5f;

    [Header("Cast Points")]
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private Transform aoeSpawnPoint;

    public bool SpawnProjectile(
        Transform ownerRoot,
        PlayerRef attackerRef,
        Vector3 aimPoint,
        bool canDealDamage,
        float burnDuration,
        float burnTickDamage,
        float burnTickInterval,
        float burnDecayFactor)
    {
        if (projectilePrefab == null)
        {
            return false;
        }

        Transform spawn = projectileSpawnPoint != null ? projectileSpawnPoint : transform;
        Vector3 toAimPoint = aimPoint - spawn.position;
        Vector3 direction = toAimPoint.sqrMagnitude > 0.0001f ? toAimPoint.normalized : spawn.forward;
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

        MageFireProjectile projectile = Instantiate(projectilePrefab, spawn.position, rotation);
        projectile.Initialize(
            ownerRoot,
            attackerRef,
            projectileDamage,
            projectileSpeed,
            projectileLifetime,
            canDealDamage,
            burnDuration,
            burnTickDamage,
            burnTickInterval,
            burnDecayFactor);

        return true;
    }

    public bool SpawnAoeZone(
        Transform ownerRoot,
        PlayerRef attackerRef,
        bool canDealDamage,
        float burnDuration,
        float burnTickDamage,
        float burnTickInterval,
        float burnDecayFactor)
    {
        if (aoeZonePrefab == null)
        {
            return false;
        }

        Transform spawn = aoeSpawnPoint != null ? aoeSpawnPoint : transform;
        GameObject zoneObject = Instantiate(aoeZonePrefab, spawn.position, Quaternion.identity);

        MageAoeFireZone zone = zoneObject.GetComponent<MageAoeFireZone>();
        if (zone == null)
        {
            zone = zoneObject.AddComponent<MageAoeFireZone>();
        }

        zone.Initialize(
            ownerRoot,
            attackerRef,
            aoeDamagePerTick,
            aoeDamageTickInterval,
            aoeLifetime,
            canDealDamage,
            burnDuration,
            burnTickDamage,
            burnTickInterval,
            burnDecayFactor,
            aoeRadius);

        return true;
    }
}
