using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ClassChoose : MonoBehaviour
{
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
        UpdateClassLabel(defaultClassName);
        RefreshAcceptState();

        SetPendingClass("Knight");
    }

    public void OnArcherClicked()
    {
        SetPendingClass("Archer");
    }

    public void OnMageClicked()
    {
        SetPendingClass("Mage");
    }

    public void OnAcceptClicked()
    {
        if (string.IsNullOrWhiteSpace(pendingClassName))
        {
            return;
        }

        confirmedClassName = pendingClassName;
        LastConfirmedClassName = confirmedClassName;

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

        bool hasValidSelection = pendingClassName == "Knight" || pendingClassName == "Archer" || pendingClassName == "Mage";
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
