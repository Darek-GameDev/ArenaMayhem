using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class WorldSpaceHealthBar : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform anchor;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.45f, 0f);

    [Header("Behavior")]
    [SerializeField] private bool faceCamera = true;
    [SerializeField] private bool hideWhenDead = false;
    [SerializeField] private bool hideForInputAuthority = false;

    [Header("Health Sources")]
    [SerializeField] private SharedModePlayerController playerController;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private EnemyHealth enemyHealth;

    [Header("UI")]
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image fillImage;

    private int lastHealth = int.MinValue;
    private int lastMaxHealth = int.MinValue;

    public void ConfigureForPlayer(SharedModePlayerController controller, bool hideLocalInputAuthority = true)
    {
        playerController = controller;
        if (playerController != null && playerHealth == null)
        {
            playerHealth = playerController.GetComponent<PlayerHealth>();
        }

        hideForInputAuthority = hideLocalInputAuthority;
    }

    public void ConfigureForEnemy(EnemyHealth enemy)
    {
        enemyHealth = enemy;
        hideForInputAuthority = false;
    }

    private void Awake()
    {
        ResolveReferences();
        EnsureVisuals();
    }

    private void Update()
    {
        ResolveReferences();
        EnsureVisuals();

        if (worldCanvas == null || healthSlider == null)
        {
            return;
        }

        UpdateTransform();

        if (!TryGetHealth(out int health, out int maxHealth, out bool isDead))
        {
            SetVisible(false);
            return;
        }

        bool shouldBeVisible = ShouldBeVisible(isDead);
        SetVisible(shouldBeVisible);

        if (!shouldBeVisible)
        {
            return;
        }

        if (health != lastHealth || maxHealth != lastMaxHealth)
        {
            lastHealth = health;
            lastMaxHealth = maxHealth;
            UpdateSlider(health, maxHealth);
        }
    }

    private void ResolveReferences()
    {
        if (anchor == null)
        {
            anchor = transform;
        }

        if (playerController == null)
        {
            playerController = GetComponent<SharedModePlayerController>();
        }

        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }

        if (enemyHealth == null)
        {
            enemyHealth = GetComponent<EnemyHealth>();
        }
    }

    private bool TryGetHealth(out int health, out int maxHealth, out bool isDead)
    {
        if (enemyHealth != null)
        {
            health = Mathf.Max(0, enemyHealth.Health);
            maxHealth = Mathf.Max(1, enemyHealth.MaxHealth);
            isDead = enemyHealth.IsDead;
            return true;
        }

        if (playerHealth != null)
        {
            health = Mathf.Max(0, playerHealth.CurrentHealth);
            maxHealth = Mathf.Max(1, playerHealth.MaxHealth);
            isDead = playerHealth.IsDead;
            return true;
        }

        if (playerController != null)
        {
            health = Mathf.Max(0, playerController.Health);
            maxHealth = Mathf.Max(1, playerController.MaxHealth);
            isDead = playerController.IsDead;
            return true;
        }

        health = 0;
        maxHealth = 1;
        isDead = false;
        return false;
    }

    private bool ShouldBeVisible(bool isDead)
    {
        if (hideWhenDead && isDead)
        {
            return false;
        }

        if (hideForInputAuthority && playerController != null && playerController.Object != null && playerController.Object.HasInputAuthority)
        {
            return false;
        }

        return true;
    }

    private void SetVisible(bool visible)
    {
        if (worldCanvas.enabled == visible)
        {
            return;
        }

        worldCanvas.enabled = visible;
    }

    private void UpdateTransform()
    {
        if (anchor != null)
        {
            worldCanvas.transform.position = anchor.position + worldOffset;
        }

        if (!faceCamera)
        {
            return;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        Vector3 toCamera = cam.transform.position - worldCanvas.transform.position;
        if (toCamera.sqrMagnitude > 0.0001f)
        {
            worldCanvas.transform.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
        }
    }

    private void UpdateSlider(int health, int maxHealth)
    {
        healthSlider.minValue = 0f;
        healthSlider.maxValue = maxHealth;
        healthSlider.value = health;

        if (fillImage != null)
        {
            float ratio = (float)health / maxHealth;
            fillImage.color = Color.Lerp(new Color(0.85f, 0.15f, 0.15f), new Color(0.1f, 0.8f, 0.25f), ratio);
        }
    }

    private void EnsureVisuals()
    {
        if (worldCanvas == null)
        {
            Transform existingCanvas = transform.Find("WorldHealthUI");
            if (existingCanvas != null)
            {
                worldCanvas = existingCanvas.GetComponent<Canvas>();
            }
        }

        if (worldCanvas == null)
        {
            GameObject canvasObject = new GameObject("WorldHealthUI", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(transform, false);
            worldCanvas = canvasObject.GetComponent<Canvas>();
            worldCanvas.renderMode = RenderMode.WorldSpace;
            worldCanvas.sortingOrder = 100;

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(120f, 16f);
            canvasObject.transform.localScale = Vector3.one * 0.01f;
        }

        if (healthSlider == null)
        {
            healthSlider = worldCanvas.GetComponentInChildren<Slider>(true);
        }

        if (healthSlider == null)
        {
            CreateSlider(worldCanvas.transform);
        }

        if (fillImage == null && healthSlider.fillRect != null)
        {
            fillImage = healthSlider.fillRect.GetComponent<Image>();
        }
    }

    private void CreateSlider(Transform parent)
    {
        GameObject root = new GameObject("HealthBar", typeof(RectTransform), typeof(Image), typeof(Slider));
        root.transform.SetParent(parent, false);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = new Vector2(120f, 14f);

        Image background = root.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.6f);

        Slider slider = root.GetComponent<Slider>();
        slider.targetGraphic = background;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        GameObject fillArea = new GameObject("FillArea", typeof(RectTransform));
        fillArea.transform.SetParent(root.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0f);
        fillAreaRect.anchorMax = new Vector2(1f, 1f);
        fillAreaRect.offsetMin = new Vector2(2f, 2f);
        fillAreaRect.offsetMax = new Vector2(-2f, -2f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Image fillGraphic = fill.GetComponent<Image>();
        fillGraphic.color = new Color(0.1f, 0.8f, 0.25f);

        slider.fillRect = fillRect;
        slider.handleRect = null;

        healthSlider = slider;
        fillImage = fillGraphic;
    }
}
