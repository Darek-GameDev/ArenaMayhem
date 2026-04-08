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

    public bool Fire(Transform ownerRoot, PlayerRef attackerRef, Vector3 aimPoint)
    {
        return SpawnProjectile(ownerRoot, attackerRef, aimPoint, true);
    }

    public bool SpawnProjectile(Transform ownerRoot, PlayerRef attackerRef, Vector3 aimPoint, bool canDealDamage)
    {
        if (projectilePrefab == null)
        {
            return false;
        }

        Transform spawn = projectileSpawnPoint != null ? projectileSpawnPoint : transform;
        Vector3 toAimPoint = aimPoint - spawn.position;
        Vector3 direction = toAimPoint.sqrMagnitude > 0.0001f ? toAimPoint.normalized : spawn.forward;
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
        BowProjectile projectile = Instantiate(projectilePrefab, spawn.position, rotation);
        projectile.Initialize(ownerRoot, attackerRef, damage, projectileSpeed, projectileLifetime, canDealDamage);
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
