using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Quản lý tất cả camera effects: Shake, FOV, Cinematic moments
/// </summary>
[DefaultExecutionOrder(10000)]
[DisallowMultipleComponent]
public class CameraEffectsController : MonoBehaviour
{
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private SharedModePlayerController sharedModeController;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Shake Settings")]
    [SerializeField] private float defaultShakeDuration = 0.35f;
    [SerializeField] private float defaultShakeAmplitude = 3.2f;
    [SerializeField] private float shakePositionStrength = 0.22f;
    [SerializeField] private float shakeRotationStrength = 4.5f;
    [SerializeField] private float damageMinShakeScale = 0.4f;
    [SerializeField] private float damageShakeMultiplier = 0.7f;
    [SerializeField] private float landingMinShakeScale = 0.45f;
    [SerializeField] private bool enableDebugLogs = false;

    [Header("FOV Settings")]
    [SerializeField] private float defaultFOV = 60f;
    [SerializeField] private float aimingFOV = 42f;
    [SerializeField] private float runningSensationFOV = 72f;
    [SerializeField] private float fovTransitionSpeed = 5.5f;

    [Header("Character Tuning")]
    [SerializeField] private bool useCharacterSpecificTuning = true;
    [SerializeField] private float knightRunFOVBoost = 4f;
    [SerializeField] private float knightBlockFOV = 46f;
    [SerializeField] private float knightShakeMultiplier = 1.2f;
    [SerializeField] private float archerAimFOV = 38f;
    [SerializeField] private float archerShakeMultiplier = 0.85f;

    [Header("Cinematic - Heavy Damage")]
    [SerializeField] private float heavyDamageThreshold = 30f;
    [SerializeField] private float heavyDamageVignetteDuration = 0.5f;

    [Header("Cinematic - Near Death")]
    [SerializeField] private float nearDeathHealthPercent = 0.2f;
    [SerializeField] private float nearDeathFOV = 40f;
    [SerializeField] private bool enableLowHealthHeartbeat = true;
    [SerializeField] private float lowHealthHeartbeatMinInterval = 0.85f;
    [SerializeField] private float lowHealthHeartbeatMaxInterval = 0.45f;
    [SerializeField] private float lowHealthHeartbeatShakeDuration = 0.08f;
    [SerializeField] private float lowHealthHeartbeatShakeAmplitude = 0.25f;

    [Header("Cinematic - Death")]
    [SerializeField] private float deathFOV = 30f;
    [SerializeField] private float deathShakeDuration = 0.65f;
    [SerializeField] private float deathShakeAmplitudeMultiplier = 3f;

    // Shake
    private float shakeTimer;
    private float currentShakeDuration;
    private float currentShakeAmplitude;
    private Transform cameraTransform;
    private Transform renderCameraTransform;

    // FOV
    private float targetFOV;
    private float currentFOV;
    private bool isAiming;
    private bool isRunning;
    private bool isBlocking;
    private float currentAimFOV;
    private float currentRunFOV;
    private float currentBlockFOV;
    private float currentShakeMultiplier;

    // Cinematic
    private float elapsedVignetteTime;
    private bool isNearDeath;
    private bool isDeadNow;
    private bool deathEffectPlayed;
    private float lowHealthHeartbeatTimer;

    private void Awake()
    {
        if (cinemachineCamera == null)
        {
            cinemachineCamera = GetComponent<CinemachineCamera>();
        }

        if (cinemachineCamera == null)
        {
            cinemachineCamera = GetComponentInChildren<CinemachineCamera>(true);
        }

        ResolveReferences();
        InitializeVariables();
    }

    private void OnEnable()
    {
        if (IsLocalAuthority())
        {
            instance = this;
        }
    }

    private void OnDisable()
    {
        if (instance == this)
        {
            instance = null;
        }

        deathEffectPlayed = false;
        lowHealthHeartbeatTimer = 0f;
    }

    private void Update()
    {
        if (!IsLocalAuthority())
            return;

        if (instance != this)
        {
            instance = this;
        }

        UpdateFOV();
        UpdateCinematic();
    }

    private void OnGUI()
    {
        if (!IsLocalAuthority())
        {
            return;
        }

        if (Event.current != null && Event.current.type != EventType.Repaint)
        {
            return;
        }

    }

    private void LateUpdate()
    {
        if (!IsLocalAuthority())
            return;

        UpdateShake();
    }

    #region SHAKE SYSTEM
    private void UpdateShake()
    {
        ResolveCameraTransform();

        if (renderCameraTransform == null)
            return;

        if (shakeTimer <= 0f)
        {
            return;
        }

        shakeTimer -= Time.deltaTime;

        if (shakeTimer <= 0f)
        {
            return;
        }

        float duration = Mathf.Max(0.0001f, currentShakeDuration);
        float normalizedTime = Mathf.Clamp01(shakeTimer / duration);
        float envelope = normalizedTime * normalizedTime;

        float seedTime = Time.unscaledTime * 30f;
        float offsetX = (Mathf.PerlinNoise(seedTime, 0f) * 2f - 1f);
        float offsetY = (Mathf.PerlinNoise(0f, seedTime) * 2f - 1f);
        float offsetZ = (Mathf.PerlinNoise(seedTime, seedTime) * 2f - 1f);

        // Capture current pose each frame so shake layers on top of Cinemachine output.
        Vector3 baseLocalPosition = renderCameraTransform.localPosition;
        Quaternion baseLocalRotation = renderCameraTransform.localRotation;

        Vector3 positionOffset = new Vector3(offsetX, offsetY, offsetZ) * (shakePositionStrength * currentShakeAmplitude * envelope);
        Vector3 rotationOffset = new Vector3(offsetY, offsetZ, offsetX) * (shakeRotationStrength * currentShakeAmplitude * envelope);

        renderCameraTransform.localPosition = baseLocalPosition + positionOffset;
        renderCameraTransform.localRotation = baseLocalRotation * Quaternion.Euler(rotationOffset);
    }

    public void Shake(float duration = -1, float amplitude = -1)
    {
        ResolveCameraTransform();

        if (renderCameraTransform == null)
            return;

        duration = duration > 0 ? duration : defaultShakeDuration;
        amplitude = amplitude > 0 ? amplitude : defaultShakeAmplitude;

        shakeTimer = duration;
        currentShakeDuration = duration;
        currentShakeAmplitude = amplitude;

        if (enableDebugLogs)
        {
            Debug.Log($"[CameraEffectsController] Shake() duration={duration:F2}, amplitude={amplitude:F2}, target={renderCameraTransform?.name}", this);
        }
    }

    public void ShakeOnDamage(int damageAmount)
    {
        float scaledAmplitude = Mathf.Max(damageMinShakeScale, Mathf.Clamp01(damageAmount / 100f)) * defaultShakeAmplitude * damageShakeMultiplier * currentShakeMultiplier;
        Shake(defaultShakeDuration, scaledAmplitude);
    }

    public void ShakeOnLanding(float fallDistance)
    {
        float scaledAmplitude = Mathf.Max(landingMinShakeScale, Mathf.Clamp01(fallDistance / 10f)) * (defaultShakeAmplitude * 0.7f) * currentShakeMultiplier;
        float duration = Mathf.Clamp(fallDistance / 10f, 0.2f, 0.45f);
        Shake(duration, scaledAmplitude);
    }
    #endregion

    #region FOV SYSTEM
    private void UpdateFOV()
    {
        UpdatePlayerState();
        CalculateTargetFOV();
        ApplyFOVTransition();
    }

    private void UpdatePlayerState()
    {
        if (sharedModeController != null)
        {
            isAiming = sharedModeController.IsAiming;
            isRunning = sharedModeController.NetLocomotionState == SharedModePlayerController.LocomotionState.Sprinting;
            isBlocking = sharedModeController.IsBlocking;
            UpdateCharacterTuning();
        }
        else
        {
            currentAimFOV = aimingFOV;
            currentRunFOV = runningSensationFOV;
            currentBlockFOV = defaultFOV;
            currentShakeMultiplier = 1f;
        }
    }

    private void CalculateTargetFOV()
    {
        if (isDeadNow)
        {
            targetFOV = deathFOV;
        }
        else if (isNearDeath)
        {
            targetFOV = nearDeathFOV;
        }
        else if (isBlocking)
        {
            targetFOV = currentBlockFOV;
        }
        else if (isAiming)
        {
            targetFOV = currentAimFOV;
        }
        else if (isRunning)
        {
            targetFOV = currentRunFOV;
        }
        else
        {
            targetFOV = defaultFOV;
        }
    }

    private void ApplyFOVTransition()
    {
        currentFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * fovTransitionSpeed);
        if (cinemachineCamera != null)
        {
            cinemachineCamera.Lens.FieldOfView = currentFOV;
        }
    }

    public void SetAiming(bool aiming)
    {
        isAiming = aiming;
    }

    public void SetRunning(bool running)
    {
        isRunning = running;
    }
    #endregion

    #region CINEMATIC SYSTEM
    private void UpdateCinematic()
    {
        if (elapsedVignetteTime > 0)
        {
            elapsedVignetteTime -= Time.deltaTime;
        }

        // Check near death every frame
        if (TryReadHealth(out int currentHealth, out int maxHp, out bool isDead))
        {
            float safeMaxHealth = Mathf.Max(1f, maxHp);
            float healthPercent = currentHealth / safeMaxHealth;
            isDeadNow = isDead;

            bool shouldBeNearDeath = healthPercent > 0f && healthPercent <= nearDeathHealthPercent && !isDead;
            
            if (shouldBeNearDeath != isNearDeath)
            {
                OnNearDeath(shouldBeNearDeath);
            }

            UpdateLowHealthHeartbeat(shouldBeNearDeath, healthPercent);

            if (isDead && !deathEffectPlayed)
            {
                OnDeath();
            }
            else if (!isDead)
            {
                deathEffectPlayed = false;
            }
        }
        else
        {
            isDeadNow = false;
            isNearDeath = false;
        }
    }

    public void OnHeavyDamage(int damageAmount)
    {
        ShakeOnDamage(damageAmount);
        elapsedVignetteTime = heavyDamageVignetteDuration;
    }

    private void OnNearDeath(bool nearDeath)
    {
        isNearDeath = nearDeath;

        if (!isNearDeath)
        {
            lowHealthHeartbeatTimer = 0f;
        }
    }

    private void UpdateLowHealthHeartbeat(bool shouldBeNearDeath, float healthPercent)
    {
        if (!enableLowHealthHeartbeat || !shouldBeNearDeath)
        {
            return;
        }

        lowHealthHeartbeatTimer -= Time.deltaTime;
        if (lowHealthHeartbeatTimer > 0f)
        {
            return;
        }

        // Avoid overriding stronger hit/landing/death shakes already in progress.
        if (shakeTimer <= 0f)
        {
            float severity = Mathf.InverseLerp(nearDeathHealthPercent, 0f, healthPercent);
            float pulseAmplitude = Mathf.Lerp(0.08f, lowHealthHeartbeatShakeAmplitude, severity) * currentShakeMultiplier;
            Shake(lowHealthHeartbeatShakeDuration, pulseAmplitude);
        }

        float nextInterval = Mathf.Lerp(lowHealthHeartbeatMinInterval, lowHealthHeartbeatMaxInterval, Mathf.InverseLerp(nearDeathHealthPercent, 0f, healthPercent));
        lowHealthHeartbeatTimer = Mathf.Max(0.15f, nextInterval);
    }

    public void OnLanding(float fallDistance)
    {
        ShakeOnLanding(fallDistance);
    }

    public void OnKillEnemy()
    {
        Shake(0.1f, 0.3f);  // Light shake on kill
    }

    private void OnDeath()
    {
        deathEffectPlayed = true;

        float deathAmplitude = Mathf.Max(defaultShakeAmplitude * deathShakeAmplitudeMultiplier, defaultShakeAmplitude);
        Shake(deathShakeDuration, deathAmplitude);

        if (enableDebugLogs)
        {
            Debug.Log($"[CameraEffectsController] Death effect triggered on {name}", this);
        }
    }

    public float GetCurrentVignetteAlpha()
    {
        return Mathf.Clamp01(elapsedVignetteTime / heavyDamageVignetteDuration);
    }

    public bool GetIsNearDeath()
    {
        return isNearDeath;
    }
    #endregion

    #region STATIC CALLBACK (for integration with PlayerHealth)
    private static CameraEffectsController instance;
    private static bool missingInstanceLogged;

    /// <summary>
    /// Static method để PlayerHealth hoặc listener script gọi
    /// </summary>
    public static void OnPlayerTakeDamage(int damageAmount)
    {
        if (instance != null)
        {
            if (instance.enableDebugLogs)
            {
                Debug.Log($"[CameraEffectsController] OnPlayerTakeDamage({damageAmount}) received by {instance.name}", instance);
            }

            instance.OnHeavyDamage(damageAmount);
            missingInstanceLogged = false;
        }
        else if (!missingInstanceLogged)
        {
            missingInstanceLogged = true;
            Debug.LogWarning("[CameraEffectsController] OnPlayerTakeDamage called but no local instance is registered yet.");
        }
    }

    public static void OnPlayerLanding(float fallDistance)
    {
        if (instance != null)
        {
            instance.ShakeOnLanding(fallDistance);
        }
    }

    public static void OnPlayerKill()
    {
        if (instance != null)
        {
            instance.OnKillEnemy();
        }
    }
    #endregion

    #region HELPER METHODS
    private void ResolveReferences()
    {
        if (sharedModeController == null)
        {
            sharedModeController = GetComponent<SharedModePlayerController>();
        }

        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }

        if (cinemachineCamera != null)
        {
            cameraTransform = cinemachineCamera.transform;
        }

        ResolveCameraTransform();
    }

    private void InitializeVariables()
    {
        targetFOV = defaultFOV;
        currentFOV = defaultFOV;
        currentAimFOV = aimingFOV;
        currentRunFOV = runningSensationFOV;
        currentBlockFOV = defaultFOV;
        currentShakeMultiplier = 1f;
        isDeadNow = false;
    }

    private bool TryReadHealth(out int currentHealth, out int maxHp, out bool isDead)
    {
        currentHealth = 0;
        maxHp = 0;
        isDead = false;

        if (sharedModeController != null)
        {
            if (sharedModeController.Object == null)
            {
                return false;
            }

            maxHp = sharedModeController.MaxHealth;
            currentHealth = sharedModeController.Health;
            isDead = sharedModeController.IsDead;
            return maxHp > 0;
        }

        if (playerHealth == null)
        {
            return false;
        }

        maxHp = playerHealth.MaxHealth;
        currentHealth = playerHealth.CurrentHealth;
        isDead = playerHealth.IsDead;
        return maxHp > 0;
    }

    private void UpdateCharacterTuning()
    {
        currentAimFOV = aimingFOV;
        currentRunFOV = runningSensationFOV;
        currentBlockFOV = defaultFOV;
        currentShakeMultiplier = 1f;

        if (!useCharacterSpecificTuning || sharedModeController == null)
        {
            return;
        }

        if (sharedModeController.WeaponType == SharedModePlayerController.PlayerWeaponType.Sword)
        {
            currentRunFOV = runningSensationFOV + knightRunFOVBoost;
            currentBlockFOV = knightBlockFOV;
            currentShakeMultiplier = knightShakeMultiplier;
            return;
        }

        if (sharedModeController.WeaponType == SharedModePlayerController.PlayerWeaponType.Bow)
        {
            currentAimFOV = archerAimFOV;
            currentShakeMultiplier = archerShakeMultiplier;
        }
    }

    private void ResolveCameraTransform()
    {
        if (renderCameraTransform != null)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            renderCameraTransform = mainCamera.transform;

            if (enableDebugLogs)
            {
                Debug.Log($"[CameraEffectsController] Using render camera: {renderCameraTransform.name}", this);
            }
            return;
        }

        if (cinemachineCamera != null)
        {
            Camera parentCamera = cinemachineCamera.GetComponentInParent<Camera>();
            if (parentCamera != null)
            {
                renderCameraTransform = parentCamera.transform;
            }
        }
    }

    private bool IsLocalAuthority()
    {
        if (sharedModeController == null)
            return true;

        return sharedModeController.Object != null && sharedModeController.Object.HasInputAuthority;
    }
    #endregion
}
