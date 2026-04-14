using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

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
        ResolveUiReferences();

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

        SharedRoomSessionManager sessionManager = SharedRoomSessionManager.EnsureInstance();
        if (sessionManager != null)
        {
            sessionManager.SetLocalClass(confirmedClassName);
            sessionManager.SetLocalReady(true);
        }

        if (lobbyUI != null)
        {
            lobbyUI.SendMessage("SetLocalPlayerClass", confirmedClassName, SendMessageOptions.DontRequireReceiver);
        }

        if (classChooseUI != null)
        {
            classChooseUI.SetActive(false);
        }

        ResolveUiReferences();

        if (lobbyUI != null)
        {
            lobbyUI.SetActive(true);
            lobbyUI.SendMessage("RefreshLobbyUI", SendMessageOptions.DontRequireReceiver);
            lobbyUI.SendMessage("SetPlayerCount", 1, SendMessageOptions.DontRequireReceiver);
        }
        else
        {
            Debug.LogWarning("ClassChoose: khong tim thay LobbyUI de hien thi sau khi Accept.");
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
        LastConfirmedClassName = null;
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

    private void ResolveUiReferences()
    {
        if (classChooseUI == null)
        {
            classChooseUI = gameObject;
        }

        if (lobbyUI != null)
        {
            return;
        }

        LobbyUI[] lobbyCandidates = Resources.FindObjectsOfTypeAll<LobbyUI>();
        Scene currentScene = gameObject.scene;

        for (int i = 0; i < lobbyCandidates.Length; i++)
        {
            LobbyUI candidate = lobbyCandidates[i];
            if (candidate == null)
            {
                continue;
            }

            GameObject candidateObject = candidate.gameObject;
            if (candidateObject == null)
            {
                continue;
            }

            if (!candidateObject.scene.IsValid() || candidateObject.scene != currentScene)
            {
                continue;
            }

            lobbyUI = candidateObject;
            return;
        }
    }
}
