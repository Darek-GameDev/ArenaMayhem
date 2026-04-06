using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class LocalCollectedItemUI : MonoBehaviour
{
    [SerializeField] private SharedModePlayerController controller;
    [SerializeField] private TMP_Text tmpLabel;
    [SerializeField] private Text legacyLabel;
    [SerializeField] private string displayFormat = "Items: {0}";

    private int lastRenderedCount = int.MinValue;

    private void Awake()
    {
        if (tmpLabel == null)
        {
            tmpLabel = GetComponent<TMP_Text>();
        }

        if (legacyLabel == null)
        {
            legacyLabel = GetComponent<Text>();
        }
    }

    private void Update()
    {
        if (controller == null || controller.Object == null || !controller.Object.HasInputAuthority)
        {
            controller = FindLocalController();
        }

        if (controller == null)
        {
            return;
        }

        int count = controller.CollectedItemCount;
        if (count == lastRenderedCount)
        {
            return;
        }

        lastRenderedCount = count;
        string value = string.Format(displayFormat, count);

        if (tmpLabel != null)
        {
            tmpLabel.text = value;
        }

        if (legacyLabel != null)
        {
            legacyLabel.text = value;
        }
    }

    private static SharedModePlayerController FindLocalController()
    {
        SharedModePlayerController[] controllers = FindObjectsByType<SharedModePlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < controllers.Length; i++)
        {
            SharedModePlayerController candidate = controllers[i];
            if (candidate != null && candidate.Object != null && candidate.Object.HasInputAuthority)
            {
                return candidate;
            }
        }

        return null;
    }
}
