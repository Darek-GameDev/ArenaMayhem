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

        if (lobbyUI != null)
        {
            lobbyUI.SetActive(false);
        }
    }

    public void OnKnightClicked()
    {
        SetPendingClass("Knight");
    }

    public void OnArcherClicked()
    {
        SetPendingClass("Archer");
    }

    public void OnAcceptClicked()
    {
        if (string.IsNullOrWhiteSpace(pendingClassName))
        {
            return;
        }

        confirmedClassName = pendingClassName;
        LastConfirmedClassName = confirmedClassName;

        if (lobbyUI != null)
        {
            LobbyUI lobbyUIScript = lobbyUI.GetComponent<LobbyUI>();
            if (lobbyUIScript != null)
            {
                lobbyUIScript.ResetLobbyUI();
            }

            lobbyUI.SendMessage("SetPlayerCount", 1, SendMessageOptions.DontRequireReceiver);
            lobbyUI.SendMessage("SetLocalPlayerClass", confirmedClassName, SendMessageOptions.DontRequireReceiver);
        }

        if (classChooseUI != null)
        {
            classChooseUI.SetActive(false);
        }

        if (lobbyUI != null)
        {
            lobbyUI.SetActive(true);
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
