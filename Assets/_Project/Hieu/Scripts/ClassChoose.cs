using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ClassChoose : MonoBehaviour
{
    private const string PlayerClassPrefsKey = "PLAYER_CLASS_NAME";

    [Header("Class Name Label")]
    [SerializeField] private TMP_Text classNameTmpText;
    [SerializeField] private Text classNameText;

    [Header("UI Flow")]
    [SerializeField] private GameObject classChooseUI;
    [SerializeField] private GameObject lobbyUI;
    [SerializeField] private Button acceptButton;

    [Header("Default Class")]
    [SerializeField] private string defaultClassName = "Class Choose";

    private string pendingClassName;
    public string SelectedClassName => confirmedClassName;
    public string PendingClassName => pendingClassName;
    public static string LastConfirmedClassName { get; private set; }

    private string confirmedClassName;

    void Start()
    {
        if (string.IsNullOrWhiteSpace(LastConfirmedClassName))
        {
            string savedClass = PlayerPrefs.GetString(PlayerClassPrefsKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(savedClass))
            {
                LastConfirmedClassName = savedClass;
            }
        }

        UpdateClassLabel(defaultClassName);
        RefreshAcceptState();

        SetPendingClass(string.IsNullOrWhiteSpace(LastConfirmedClassName) ? "Knight" : LastConfirmedClassName);
    }

    public void OnArcherClicked()
    {
        SetPendingClass("Archer");
    }

    public void OnKnightClicked()
    {
        SetPendingClass("Knight");
    }
    

    public void OnAcceptClicked()
    {
        if (string.IsNullOrWhiteSpace(pendingClassName))
        {
            return;
        }

        confirmedClassName = pendingClassName;
        LastConfirmedClassName = confirmedClassName;
        PlayerPrefs.SetString(PlayerClassPrefsKey, confirmedClassName);
        PlayerPrefs.Save();

        SharedRoomSessionManager sessionManager = SharedRoomSessionManager.EnsureInstance();
        if (sessionManager != null)
        {
            sessionManager.SetLocalClass(confirmedClassName);
            sessionManager.SetLocalReady(true);
            sessionManager.FlushLocalStateToSession();
        }

        // Activate lobby UI before calling SetLocalPlayerClass to ensure Coroutine runs.
        if (lobbyUI != null)
        {
            lobbyUI.SetActive(true);
            LobbyUI lobbyUIScript = lobbyUI.GetComponent<LobbyUI>();
            if (lobbyUIScript != null)
            {
                lobbyUIScript.SetLocalPlayerClass(confirmedClassName);
            }
        }

        // Close class choose UI.
        if (classChooseUI != null)
        {
            classChooseUI.SetActive(false);
        }
    }

    public void SetPendingClass(string className)
    {
        if (string.IsNullOrWhiteSpace(className))
        {
            return;
        }

        pendingClassName = className;
        UpdateClassLabel(className);
        RefreshAcceptState();
    }

    public void ResetSelectionUI()
    {
        pendingClassName = null;
        confirmedClassName = null;
        UpdateClassLabel(defaultClassName);
        RefreshAcceptState();
    }

    private void RefreshAcceptState()
    {
        if (acceptButton == null)
        {
            return;
        }

        bool hasValidSelection = pendingClassName == "Knight" || pendingClassName == "Archer";
        acceptButton.interactable = hasValidSelection;
    }

    private void UpdateClassLabel(string className)
    {
        if (string.IsNullOrWhiteSpace(className))
        {
            className = defaultClassName;
        }

        if (classNameTmpText != null)
        {
            classNameTmpText.text = className;
        }

        if (classNameText != null)
        {
            classNameText.text = className;
        }
    }
}
