using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class HealthUI : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth = 100;
    [SerializeField] private int damagePerClick = 10;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private KeyCode demoDamageKey = KeyCode.R;
    [SerializeField] private bool useNormalizedSlider = false;

    private int lastHealth = int.MinValue;
    private int lastMaxHealth = int.MinValue;

    private void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (healthText == null)
        {
            healthText = GetComponentInChildren<TMP_Text>(true);
        }

        if (healthSlider == null)
        {
            healthSlider = GetComponentInChildren<Slider>(true);
        }

        RefreshUI(true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(demoDamageKey))
        {
            TestReduceHealth();
        }

        if (currentHealth == lastHealth && maxHealth == lastMaxHealth)
        {
            return;
        }

        RefreshUI();
    }

    public void TestReduceHealth()
    {
        if (currentHealth <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - Mathf.Max(1, damagePerClick));
        RefreshUI(true);
    }

    public void TestResetHealth()
    {
        currentHealth = maxHealth;
        RefreshUI(true);
    }

    private void RefreshUI(bool force = false)
    {
        if (!force && currentHealth == lastHealth && maxHealth == lastMaxHealth)
        {
            return;
        }

        lastHealth = currentHealth;
        lastMaxHealth = maxHealth;

        if (healthText != null)
        {
            healthText.text = $"{currentHealth} / {maxHealth}";
        }

        if (healthSlider != null)
        {
            if (useNormalizedSlider)
            {
                healthSlider.minValue = 0f;
                healthSlider.maxValue = 1f;
                healthSlider.value = (float)currentHealth / maxHealth;
            }
            else
            {
                healthSlider.minValue = 0f;
                healthSlider.maxValue = maxHealth;
                healthSlider.value = currentHealth;
            }
        }
    }
}
