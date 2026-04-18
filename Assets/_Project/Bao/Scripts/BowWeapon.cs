using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
public class BowWeapon : MonoBehaviour
{
    public enum ProjectileType : byte
    {
        Primary = 0,
        Secondary = 1,
    }

    [Header("Primary Projectile")]
    [SerializeField] private BowProjectile projectilePrefab;
    [SerializeField] private float projectileSpeed = 28f;
    [SerializeField] private float projectileLifetime = 4f;
    [SerializeField] private int damage = 1;

    [Header("Secondary Projectile")]
    [SerializeField] private BowProjectile secondaryProjectilePrefab;
    [SerializeField] private float secondaryProjectileSpeed = 22f;
    [SerializeField] private float secondaryProjectileLifetime = 5f;
    [SerializeField] private int secondaryDamage = 2;

    [Header("Skill Projectile")]
    [SerializeField] private GameObject skillShotProjectilePrefab;
    [SerializeField] private float skillShotProjectileSpeed = 28f;
    [SerializeField] private float skillShotProjectileLifetime = 4f;
    [SerializeField] private int skillShotDamage = 1;
    [SerializeField] private float skillShotFreezeDuration = 2.5f;

    [SerializeField] private Transform projectileSpawnPoint;

    public bool Fire(Transform ownerRoot, PlayerRef attackerRef, Vector3 aimPoint)
    {
        return SpawnProjectile(ownerRoot, attackerRef, aimPoint, true, ProjectileType.Primary);
    }

    public bool FireSecondary(Transform ownerRoot, PlayerRef attackerRef, Vector3 aimPoint)
    {
        return SpawnProjectile(ownerRoot, attackerRef, aimPoint, true, ProjectileType.Secondary);
    }

    public bool FireSkillShot(Transform ownerRoot, PlayerRef attackerRef, Vector3 aimPoint, bool canDealDamage = true)
    {
        return SpawnSkillProjectile(ownerRoot, attackerRef, aimPoint, canDealDamage);
    }

    public bool SpawnProjectile(Transform ownerRoot, PlayerRef attackerRef, Vector3 aimPoint, bool canDealDamage)
    {
        return SpawnProjectile(ownerRoot, attackerRef, aimPoint, canDealDamage, ProjectileType.Primary);
    }

    public bool SpawnProjectile(Transform ownerRoot, PlayerRef attackerRef, Vector3 aimPoint, bool canDealDamage, ProjectileType projectileType)
    {
        return SpawnProjectile(ownerRoot, attackerRef, aimPoint, canDealDamage, projectileType, false);
    }

    public bool SpawnProjectile(Transform ownerRoot, PlayerRef attackerRef, Vector3 aimPoint, bool canDealDamage, ProjectileType projectileType, bool spawnSkillEffect)
    {
        BowProjectile chosenPrefab = projectilePrefab;

        if (projectileType == ProjectileType.Secondary && secondaryProjectilePrefab != null)
        {
            chosenPrefab = secondaryProjectilePrefab;
        }

        if (chosenPrefab == null)
        {
            return false;
        }

        float speed = projectileSpeed;
        float lifetime = projectileLifetime;
        int projectileDamage = damage;

        if (projectileType == ProjectileType.Secondary)
        {
            speed = secondaryProjectileSpeed;
            lifetime = secondaryProjectileLifetime;
            projectileDamage = secondaryDamage;
        }

        Transform spawn = projectileSpawnPoint != null ? projectileSpawnPoint : transform;
        Vector3 toAimPoint = aimPoint - spawn.position;
        Vector3 direction = toAimPoint.sqrMagnitude > 0.0001f ? toAimPoint.normalized : spawn.forward;
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
        BowProjectile projectile = Instantiate(chosenPrefab, spawn.position, rotation);
        projectile.Initialize(ownerRoot, attackerRef, projectileDamage, speed, lifetime, canDealDamage);

        return true;
    }

    public bool SpawnSkillProjectile(Transform ownerRoot, PlayerRef attackerRef, Vector3 aimPoint, bool canDealDamage = true)
    {
        GameObject chosenPrefab = skillShotProjectilePrefab != null ? skillShotProjectilePrefab : projectilePrefab != null ? projectilePrefab.gameObject : null;
        if (chosenPrefab == null)
        {
            return false;
        }

        Transform spawn = projectileSpawnPoint != null ? projectileSpawnPoint : transform;
        Vector3 toAimPoint = aimPoint - spawn.position;
        Vector3 direction = toAimPoint.sqrMagnitude > 0.0001f ? toAimPoint.normalized : spawn.forward;
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
        GameObject projectileObject = Instantiate(chosenPrefab, spawn.position, rotation);

        BowProjectile bowProjectile = projectileObject.GetComponent<BowProjectile>();
        if (bowProjectile != null)
        {
            bowProjectile.Initialize(
                ownerRoot,
                attackerRef,
                skillShotDamage,
                skillShotProjectileSpeed,
                skillShotProjectileLifetime,
                canDealDamage,
                true,
                skillShotFreezeDuration);
        }
        else
        {
            Rigidbody body = projectileObject.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.linearVelocity = direction * Mathf.Max(0.1f, skillShotProjectileSpeed);
            }

            Destroy(projectileObject, Mathf.Max(0.1f, skillShotProjectileLifetime));
        }

        return true;
    }

    public static Vector3 GetAimPointFromCamera(Transform ownerRoot, float rayDistance, LayerMask layerMask)
    {
        Camera mainCam = Camera.main;
        float clampedDistance = Mathf.Max(1f, rayDistance);

        if (mainCam == null)
        {
            return ownerRoot != null ? ownerRoot.position + ownerRoot.forward * clampedDistance : Vector3.forward * clampedDistance;
        }

        Ray screenCenterRay = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit[] hits = Physics.RaycastAll(screenCenterRay, clampedDistance, layerMask, QueryTriggerInteraction.Ignore);
        if (hits != null && hits.Length > 0)
        {
            System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                Collider hitCollider = hits[i].collider;
                if (hitCollider == null)
                {
                    continue;
                }

                if (ownerRoot != null && hitCollider.transform.root == ownerRoot.root)
                {
                    continue;
                }

                return hits[i].point;
            }
        }

        return screenCenterRay.origin + screenCenterRay.direction * clampedDistance;
    }
}
