using UnityEngine;

/// <summary>
/// Tự động kết nối PlayerHealth với CameraEffectsController
/// Không cần chỉnh sửa script ở ngoài folder Camera
/// Script này detect khi player nhận damage và gọi camera effects
/// </summary>
[DisallowMultipleComponent]
public class PlayerHealthCameraConnector : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private SharedModePlayerController sharedModeController;
    [SerializeField] private bool enableDebugLogs = false;
    private int lastRecordedHealth;
    private bool localInitialized;

    private void Awake()
    {
        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }

        if (playerHealth == null)
        {
            Debug.LogWarning($"PlayerHealth không tìm thấy trên {gameObject.name}. Disabling {GetType().Name}");
            enabled = false;
            return;
        }

        if (sharedModeController == null)
        {
            sharedModeController = GetComponent<SharedModePlayerController>();
        }

        lastRecordedHealth = 0;
    }

    private void Update()
    {
        if (playerHealth == null)
            return;

        // In Shared Mode, Object may not be ready in Awake.
        if (sharedModeController != null && sharedModeController.Object == null)
            return;

        if (!IsLocalAuthority())
            return;

        if (!localInitialized)
        {
            if (!TryReadHealth(out int initialHealth, out int initialMaxHealth))
            {
                return;
            }

            lastRecordedHealth = initialHealth;
            localInitialized = true;

            if (enableDebugLogs)
            {
                Debug.Log($"[PlayerHealthCameraConnector] Local initialized on {name}, hp={initialHealth}/{initialMaxHealth}", this);
            }
            return;
        }

        if (!TryReadHealth(out int currentHealth, out int maxHealth))
        {
            return;
        }

        // Detect khi health thay đổi (nhận damage)
        if (maxHealth > 0 && currentHealth < lastRecordedHealth)
        {
            int damageAmount = lastRecordedHealth - currentHealth;

            if (enableDebugLogs)
            {
                Debug.Log($"[PlayerHealthCameraConnector] Damage detected on {name}: {damageAmount} ({lastRecordedHealth}->{currentHealth})", this);
            }

            CameraEffectsController.OnPlayerTakeDamage(damageAmount);
        }

        lastRecordedHealth = currentHealth;
    }

    private bool TryReadHealth(out int currentHealth, out int maxHealth)
    {
        currentHealth = 0;
        maxHealth = 0;

        if (sharedModeController != null)
        {
            if (sharedModeController.Object == null)
            {
                return false;
            }

            currentHealth = sharedModeController.Health;
            maxHealth = sharedModeController.MaxHealth;
            return maxHealth > 0;
        }

        if (playerHealth == null)
        {
            return false;
        }

        currentHealth = playerHealth.CurrentHealth;
        maxHealth = playerHealth.MaxHealth;
        return maxHealth > 0;
    }

    private bool IsLocalAuthority()
    {
        if (sharedModeController == null)
        {
            return true;
        }

        return sharedModeController.Object != null && sharedModeController.Object.HasInputAuthority;
    }
}
