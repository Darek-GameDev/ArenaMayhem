using UnityEngine;

[DisallowMultipleComponent]
public class Lava : MonoBehaviour
{
    [SerializeField] private int instantKillDamage = int.MaxValue;
    [SerializeField] private bool letDeadPlayerFallThroughLava = true;

    private Collider[] lavaColliders;

    private void Awake()
    {
        lavaColliders = GetComponents<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryKill(other != null ? other.GetComponentInParent<PlayerHealth>() : null);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryKill(collision != null ? collision.collider.GetComponentInParent<PlayerHealth>() : null);
    }

    private void TryKill(PlayerHealth playerHealth)
    {
        if (playerHealth == null || playerHealth.IsDead)
        {
            return;
        }

        playerHealth.TakeDamage(instantKillDamage);

        if (letDeadPlayerFallThroughLava)
        {
            IgnorePlayerVsLavaCollision(playerHealth);
        }
    }

    private void IgnorePlayerVsLavaCollision(PlayerHealth playerHealth)
    {
        if (lavaColliders == null || lavaColliders.Length == 0)
        {
            return;
        }

        Collider[] playerColliders = playerHealth.GetComponentsInChildren<Collider>(true);
        foreach (Collider lavaCollider in lavaColliders)
        {
            if (lavaCollider == null)
            {
                continue;
            }

            foreach (Collider playerCollider in playerColliders)
            {
                if (playerCollider == null)
                {
                    continue;
                }

                Physics.IgnoreCollision(playerCollider, lavaCollider, true);
            }

            CharacterController controller = playerHealth.GetComponent<CharacterController>();
            if (controller != null)
            {
                Physics.IgnoreCollision(controller, lavaCollider, true);
            }
        }
    }
}