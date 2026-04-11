using UnityEngine;
using DG.Tweening;

public class Dotween : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private RectTransform panelTransform;
    [SerializeField] private CanvasGroup panelCanvasGroup;

    [Header("Animation")]
    [SerializeField] private float showDuration = 0.28f;
    [SerializeField] private float hideDuration = 0.2f;
    [SerializeField] private float hiddenScale = 0.8f;
    [SerializeField] private Ease showEase = Ease.OutBack;
    [SerializeField] private Ease hideEase = Ease.InBack;

    private Sequence currentSequence;

    private bool IsTargetReady()
    {
        return settingsPanel != null && panelTransform != null && panelCanvasGroup != null;
    }

    private void Awake()
    {
        if (settingsPanel == null)
        {
            settingsPanel = gameObject;
        }

        if (panelTransform == null && settingsPanel != null)
        {
            panelTransform = settingsPanel.GetComponent<RectTransform>();
        }

        if (panelCanvasGroup == null && settingsPanel != null)
        {
            panelCanvasGroup = settingsPanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
            {
                panelCanvasGroup = settingsPanel.AddComponent<CanvasGroup>();
            }
        }

        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            panelTransform.localScale = Vector3.one;
            panelCanvasGroup.alpha = 1f;
            panelCanvasGroup.interactable = true;
            panelCanvasGroup.blocksRaycasts = true;
        }
        else if (settingsPanel != null)
        {
            panelTransform.localScale = Vector3.one * hiddenScale;
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;
        }
    }

    public void ShowSettings()
    {
        if (!IsTargetReady())
        {
            return;
        }

        if (settingsPanel.activeSelf && panelCanvasGroup.alpha >= 0.99f)
        {
            return;
        }

        currentSequence?.Kill();

        settingsPanel.SetActive(true);
        panelTransform.localScale = Vector3.one * hiddenScale;
        panelCanvasGroup.alpha = 0f;
        panelCanvasGroup.interactable = false;
        panelCanvasGroup.blocksRaycasts = false;

        currentSequence = DOTween.Sequence();
        currentSequence.Join(panelTransform.DOScale(1f, showDuration).SetEase(showEase));
        currentSequence.Join(panelCanvasGroup.DOFade(1f, showDuration));
        currentSequence.OnComplete(() =>
        {
            panelCanvasGroup.interactable = true;
            panelCanvasGroup.blocksRaycasts = true;
        });
    }

    public void HideSettings()
    {
        if (!IsTargetReady() || !settingsPanel.activeSelf)
        {
            return;
        }

        currentSequence?.Kill();

        panelCanvasGroup.interactable = false;
        panelCanvasGroup.blocksRaycasts = false;

        currentSequence = DOTween.Sequence();
        currentSequence.Join(panelTransform.DOScale(hiddenScale, hideDuration).SetEase(hideEase));
        currentSequence.Join(panelCanvasGroup.DOFade(0f, hideDuration));
        currentSequence.OnComplete(() =>
        {
            settingsPanel.SetActive(false);
        });
    }

    public void ToggleSettings()
    {
        if (settingsPanel == null)
        {
            return;
        }

        if (settingsPanel.activeSelf)
        {
            HideSettings();
        }
        else
        {
            ShowSettings();
        }
    }

    public bool IsSettingsVisible()
    {
        return settingsPanel != null && settingsPanel.activeSelf;
    }

    public void HideSettingsImmediate()
    {
        if (!IsTargetReady())
        {
            return;
        }

        currentSequence?.Kill();
        panelTransform.localScale = Vector3.one * hiddenScale;
        panelCanvasGroup.alpha = 0f;
        panelCanvasGroup.interactable = false;
        panelCanvasGroup.blocksRaycasts = false;
        settingsPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        currentSequence?.Kill();
    }

    private void OnDisable()
    {
        currentSequence?.Kill();
    }
}
