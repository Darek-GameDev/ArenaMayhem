using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KillFeedUI : MonoBehaviour
{
    [Header("Output")]
    [SerializeField] private TMP_Text killFeedTmpText;
    [SerializeField] private Text killFeedLegacyText;

    [Header("Timing")]
    [SerializeField] private float messageDuration = 2.5f;

    private float hideAt;

    public static KillFeedUI Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        ApplyText(string.Empty);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (hideAt <= 0f)
        {
            return;
        }

        if (Time.unscaledTime < hideAt)
        {
            return;
        }

        hideAt = 0f;
        ApplyText(string.Empty);
    }

    public static void ShowMessage(string message)
    {
        if (Instance == null)
        {
            Debug.Log($"KillFeed: {message}");
            return;
        }

        Instance.ShowInternal(message);
    }

    private void ShowInternal(string message)
    {
        string safe = string.IsNullOrWhiteSpace(message) ? string.Empty : message.Trim();
        ApplyText(safe);
        hideAt = Time.unscaledTime + Mathf.Max(0.5f, messageDuration);
    }

    private void ApplyText(string value)
    {
        if (killFeedTmpText != null)
        {
            killFeedTmpText.text = value;
        }

        if (killFeedLegacyText != null)
        {
            killFeedLegacyText.text = value;
        }
    }
}
