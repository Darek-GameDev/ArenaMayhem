using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class LocalPlayerHealthSlider : MonoBehaviour
{
    [SerializeField] private SharedModePlayerController controller;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private bool useNormalizedValue = false;

    private int lastHealth = int.MinValue;
    private int lastMaxHealth = int.MinValue;

    private void Awake()
    {
        if (healthSlider == null)
        {
            healthSlider = GetComponent<Slider>();
        }
    }

    private void Update()
    {
        ResolveReferences();

        if (healthSlider == null || playerHealth == null)
        {
            return;
        }

        int currentHealth = Mathf.Max(0, playerHealth.CurrentHealth);
        int maxHealth = Mathf.Max(1, playerHealth.MaxHealth);

        if (currentHealth == lastHealth && maxHealth == lastMaxHealth)
        {
            return;
        }

        lastHealth = currentHealth;
        lastMaxHealth = maxHealth;

        if (useNormalizedValue)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.value = (float)currentHealth / maxHealth;
            return;
        }

        healthSlider.minValue = 0f;
        healthSlider.maxValue = maxHealth;
        healthSlider.value = currentHealth;
    }

    private void ResolveReferences()
    {
        if (controller == null || controller.Object == null || !controller.Object.HasInputAuthority)
        {
            controller = FindLocalController();
        }

        if (controller != null)
        {
            if (playerHealth == null)
            {
                playerHealth = controller.GetComponent<PlayerHealth>();
            }

            return;
        }

        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>(FindObjectsInactive.Exclude);
        }
    }

    private SharedModePlayerController FindLocalController()
    {
        SharedModePlayerController[] players = FindObjectsByType<SharedModePlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
        {
            SharedModePlayerController candidate = players[i];
            if (candidate != null && candidate.Object != null && candidate.Object.HasInputAuthority)
            {
                return candidate;
            }
        }

        return null;
    }
}
