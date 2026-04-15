using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
public class BowWeapon : MonoBehaviour
{
    public enum ProjectileType : byte
    {
        Primary = 0,
        Secondary = 1,
        Freeze = 2,
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

    [Header("Freeze Projectile")]
    [SerializeField] private BowProjectile freezeProjectilePrefab;
    [SerializeField] private float freezeProjectileSpeed = 26f;
    [SerializeField] private float freezeProjectileLifetime = 4f;
    [SerializeField] private int freezeDamage = 1;
    [SerializeField] private float freezeDuration = 2f;

    [Header("Skill Shot Effect")]
    [SerializeField] private GameObject skillShotEffectPrefab;
    [SerializeField] private float skillShotEffectLifetime = 2f;

    [SerializeField] private Transform projectileSpawnPoint;

    public bool Fire(Transform ownerRoot, PlayerRef attackerRef, Vector3 aimPoint)
    {
        return SpawnProjectile(ownerRoot, attackerRef, aimPoint, true, ProjectileType.Primary);
    }

    public bool FireSecondary(Transform ownerRoot, PlayerRef attackerRef, Vector3 aimPoint)
    {
        return SpawnProjectile(ownerRoot, attackerRef, aimPoint, true, ProjectileType.Secondary);
    }

    public bool FireFreezeArrow(Transform ownerRoot, PlayerRef attackerRef, Vector3 aimPoint)
    {
        return SpawnProjectile(ownerRoot, attackerRef, aimPoint, true, ProjectileType.Freeze);
    }

    public bool FireSkillShot(Transform ownerRoot, PlayerRef attackerRef, Vector3 aimPoint)
    {
        return SpawnProjectile(ownerRoot, attackerRef, aimPoint, true, ProjectileType.Primary, true);
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
        else if (projectileType == ProjectileType.Freeze && freezeProjectilePrefab != null)
        {
            chosenPrefab = freezeProjectilePrefab;
        }

        if (chosenPrefab == null)
        {
            return false;
        }

        float speed = projectileSpeed;
        float lifetime = projectileLifetime;
        int projectileDamage = damage;
        bool appliesFreeze = false;
        float appliedFreezeDuration = 0f;

        if (projectileType == ProjectileType.Secondary)
        {
            speed = secondaryProjectileSpeed;
            lifetime = secondaryProjectileLifetime;
            projectileDamage = secondaryDamage;
        }
        else if (projectileType == ProjectileType.Freeze)
        {
            speed = freezeProjectileSpeed;
            lifetime = freezeProjectileLifetime;
            projectileDamage = freezeDamage;
            appliesFreeze = true;
            appliedFreezeDuration = freezeDuration;
        }

        Transform spawn = projectileSpawnPoint != null ? projectileSpawnPoint : transform;
        Vector3 toAimPoint = aimPoint - spawn.position;
        Vector3 direction = toAimPoint.sqrMagnitude > 0.0001f ? toAimPoint.normalized : spawn.forward;
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
        BowProjectile projectile = Instantiate(chosenPrefab, spawn.position, rotation);
        projectile.Initialize(ownerRoot, attackerRef, projectileDamage, speed, lifetime, canDealDamage, appliesFreeze, appliedFreezeDuration);

        if (spawnSkillEffect && skillShotEffectPrefab != null)
        {
            GameObject effectInstance = Instantiate(skillShotEffectPrefab, spawn.position, rotation);
            Destroy(effectInstance, Mathf.Max(0.1f, skillShotEffectLifetime));
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
