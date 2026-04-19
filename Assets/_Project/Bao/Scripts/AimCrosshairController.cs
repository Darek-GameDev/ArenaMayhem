using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class AimCrosshairController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerAttack localAttack;
    [SerializeField] private SharedModePlayerController sharedModeController;
    [SerializeField] private GameObject crosshairRoot;
    [SerializeField] private RawImage crosshairRawImage;

    [Header("Scene Lookup")]
    [SerializeField] private string crosshairObjectName = "Crosshair";
    [SerializeField] private float resolveRetryInterval = 0.5f;

    [Header("Animation")]
    [SerializeField] private float showAnimationDuration = 0.25f;
    [SerializeField] private float hideAnimationDuration = 0.15f;
    [SerializeField] private float minScale = 0.6f;

    private Coroutine animationCoroutine;
    private Vector3 originalScale = Vector3.one;
    private bool isVisible;
    private float nextResolveTime;

    private void Awake()
    {
        if (localAttack == null)
        {
            localAttack = GetComponent<PlayerAttack>();
        }

        if (sharedModeController == null)
        {
            sharedModeController = GetComponent<SharedModePlayerController>();
        }

        if (crosshairRoot == null && crosshairRawImage != null)
        {
            crosshairRoot = crosshairRawImage.gameObject;
        }

        TryResolveCrosshairReferences();
        HideImmediately();
    }

    private void Update()
    {
        if (!HasLocalAuthority())
        {
            return;
        }

        if ((crosshairRoot == null || crosshairRawImage == null) && Time.unscaledTime >= nextResolveTime)
        {
            nextResolveTime = Time.unscaledTime + Mathf.Max(0.1f, resolveRetryInterval);
            TryResolveCrosshairReferences();
        }

        if (crosshairRoot == null && crosshairRawImage == null)
        {
            return;
        }

        bool shouldShow = IsAimingNow();
        if (shouldShow != isVisible)
        {
            isVisible = shouldShow;
            SetCrosshairVisible(shouldShow);
        }
    }

    private bool HasLocalAuthority()
    {
        if (sharedModeController == null)
        {
            return true;
        }

        return sharedModeController.Object != null && sharedModeController.Object.HasInputAuthority;
    }

    private bool IsAimingNow()
    {
        if (sharedModeController != null)
        {
            bool hasInputAuthority = sharedModeController.Object != null && sharedModeController.Object.HasInputAuthority;
            if (!hasInputAuthority)
            {
                return false;
            }

            if (sharedModeController.UsesMagic)
            {
                return true;
            }

            return sharedModeController.IsAiming;
        }

        return localAttack != null && localAttack.IsAiming;
    }

    private void SetCrosshairVisible(bool visible)
    {
        if (crosshairRoot == null && crosshairRawImage == null)
        {
            return;
        }

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }

        if (visible)
        {
            if (crosshairRoot != null)
            {
                crosshairRoot.SetActive(true);
            }
            animationCoroutine = StartCoroutine(AnimateShow());
        }
        else
        {
            animationCoroutine = StartCoroutine(AnimateHide());
        }
    }

    private IEnumerator AnimateShow()
    {
        float duration = Mathf.Max(0.01f, showAnimationDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t); // Smooth easing

            if (crosshairRoot != null)
            {
                Vector3 scale = Vector3.Lerp(originalScale * minScale, originalScale, t);
                crosshairRoot.transform.localScale = scale;
            }

            if (crosshairRawImage != null)
            {
                Color color = crosshairRawImage.color;
                color.a = t;
                crosshairRawImage.color = color;
            }

            yield return null;
        }

        // Ensure final state
        if (crosshairRoot != null)
        {
            crosshairRoot.transform.localScale = originalScale;
        }
        if (crosshairRawImage != null)
        {
            Color color = crosshairRawImage.color;
            color.a = 1f;
            crosshairRawImage.color = color;
        }
    }

    private IEnumerator AnimateHide()
    {
        float duration = Mathf.Max(0.01f, hideAnimationDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t); // Smooth easing

            if (crosshairRoot != null)
            {
                Vector3 scale = Vector3.Lerp(originalScale, originalScale * minScale, t);
                crosshairRoot.transform.localScale = scale;
            }

            if (crosshairRawImage != null)
            {
                Color color = crosshairRawImage.color;
                color.a = 1f - t;
                crosshairRawImage.color = color;
            }

            yield return null;
        }

        // Ensure final state and deactivate
        if (crosshairRoot != null)
        {
            crosshairRoot.SetActive(false);
        }
        if (crosshairRawImage != null)
        {
            Color color = crosshairRawImage.color;
            color.a = 0f;
            crosshairRawImage.color = color;
        }
    }

    private void TryResolveCrosshairReferences()
    {
        if (crosshairRawImage == null && crosshairRoot != null)
        {
            crosshairRawImage = crosshairRoot.GetComponent<RawImage>();
        }

        if (crosshairRoot == null && crosshairRawImage != null)
        {
            crosshairRoot = crosshairRawImage.gameObject;
        }

        if (crosshairRoot != null && crosshairRawImage != null)
        {
            originalScale = crosshairRoot.transform.localScale;
            return;
        }

        RawImage[] rawImages = Object.FindObjectsByType<RawImage>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        RawImage fallback = null;
        for (int i = 0; i < rawImages.Length; i++)
        {
            RawImage candidate = rawImages[i];
            if (candidate == null)
            {
                continue;
            }

            if (candidate.gameObject.name.Equals(crosshairObjectName, System.StringComparison.OrdinalIgnoreCase))
            {
                crosshairRawImage = candidate;
                crosshairRoot = candidate.gameObject;
                originalScale = crosshairRoot.transform.localScale;
                return;
            }

            if (fallback == null && candidate.gameObject.name.ToLowerInvariant().Contains("crosshair"))
            {
                fallback = candidate;
            }
        }

        if (fallback != null)
        {
            crosshairRawImage = fallback;
            crosshairRoot = fallback.gameObject;
            originalScale = crosshairRoot.transform.localScale;
        }
    }

    private void HideImmediately()
    {
        if (crosshairRoot != null)
        {
            crosshairRoot.transform.localScale = originalScale;
            crosshairRoot.SetActive(false);
        }

        if (crosshairRawImage != null)
        {
            Color color = crosshairRawImage.color;
            color.a = 0f;
            crosshairRawImage.color = color;
        }

        isVisible = false;
        animationCoroutine = null;
        nextResolveTime = 0f;
        }
}

