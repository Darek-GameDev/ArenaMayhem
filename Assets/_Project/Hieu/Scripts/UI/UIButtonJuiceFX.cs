using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class UIButtonJuiceFX : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerClickHandler
{
    [Header("References")]
    [SerializeField] private RectTransform targetTransform;
    [SerializeField] private Button button;

    [Header("Scale")]
    [SerializeField] private float hoverScale = 1.06f;
    [SerializeField] private float pressedScale = 0.94f;
    [SerializeField] private float animDuration = 0.12f;

    [Header("Click Pulse")]
    [SerializeField] private bool useClickPulse = true;
    [SerializeField] private float clickPulseStrength = 0.08f;
    [SerializeField] private float clickPulseDuration = 0.2f;

    private Tween scaleTween;
    private Vector3 baseScale;
    private bool isPointerInside;

    private void Awake()
    {
        if (targetTransform == null)
        {
            targetTransform = GetComponent<RectTransform>();
        }

        if (button == null)
        {
            button = GetComponent<Button>();
        }

        baseScale = targetTransform.localScale;
    }

    private void OnEnable()
    {
        ResetState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!CanInteract())
        {
            return;
        }

        isPointerInside = true;
        AnimateToScale(baseScale * hoverScale, Ease.OutCubic);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!CanInteract())
        {
            return;
        }

        isPointerInside = false;
        AnimateToScale(baseScale, Ease.OutCubic);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!CanInteract())
        {
            return;
        }

        AnimateToScale(baseScale * pressedScale, Ease.OutQuad);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!CanInteract())
        {
            return;
        }

        if (isPointerInside)
        {
            AnimateToScale(baseScale * hoverScale, Ease.OutCubic);
        }
        else
        {
            AnimateToScale(baseScale, Ease.OutCubic);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!CanInteract() || !useClickPulse)
        {
            return;
        }

        targetTransform.DOKill();
        targetTransform.localScale = baseScale;
        targetTransform
            .DOPunchScale(Vector3.one * clickPulseStrength, clickPulseDuration, 10, 0.7f)
            .SetUpdate(true);
    }

    private void OnDisable()
    {
        scaleTween?.Kill();

        if (targetTransform != null)
        {
            targetTransform.localScale = baseScale;
        }
    }

    private bool CanInteract()
    {
        return button == null || button.interactable;
    }

    private void ResetState()
    {
        scaleTween?.Kill();

        if (targetTransform != null)
        {
            targetTransform.localScale = baseScale;
        }
    }

    private void AnimateToScale(Vector3 targetScale, Ease ease)
    {
        scaleTween?.Kill();
        scaleTween = targetTransform
            .DOScale(targetScale, animDuration)
            .SetEase(ease)
            .SetUpdate(true);
    }
}
