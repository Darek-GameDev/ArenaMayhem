using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MatchResultUIController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool enableSystem = true;
    [SerializeField] private int maxPlayers = 6;
    [SerializeField] [Min(2)] private int minimumPlayersToStartResult = 2;

    [Header("Result UI References")]
    [SerializeField] private GameObject resultRoot;
    [SerializeField] private GameObject winObject;
    [SerializeField] private GameObject loseObject;

    [Header("Return To Menu")]
    [SerializeField] private string menuSceneName = "Menu";
    [SerializeField] private string returnButtonName = "Return To Menu";

    [Header("Button Actions")]
    [SerializeField] private bool autoBindResultButtons = true;

    [Header("Visibility")]
    [SerializeField] private bool keepKillFeedVisible = true;

    private SharedModePlayerController localController;
    private bool resultShown;
    private bool matchArmed;
    private bool actionInProgress;

    private void Awake()
    {
        HideResultUI();

        if (autoBindResultButtons)
        {
            BindResultButtons();
        }
    }

    private void Update()
    {
        if (!enableSystem || resultShown)
        {
            return;
        }

        if (localController == null || localController.Object == null || !localController.Object.HasInputAuthority)
        {
            localController = FindLocalController();
        }

        if (localController == null)
        {
            return;
        }

        TryShowResult();
    }

    private void TryShowResult()
    {
        if (resultRoot == null || winObject == null || loseObject == null)
        {
            return;
        }

        SharedModePlayerController[] controllers = FindObjectsByType<SharedModePlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        List<SharedModePlayerController> players = new List<SharedModePlayerController>(Mathf.Max(1, maxPlayers));

        for (int i = 0; i < controllers.Length; i++)
        {
            SharedModePlayerController candidate = controllers[i];
            if (candidate == null || candidate.Object == null || !candidate.Object.IsValid || !candidate.Object.InputAuthority.IsRealPlayer)
            {
                continue;
            }

            players.Add(candidate);
        }

        if (players.Count == 0)
        {
            return;
        }

        if (!matchArmed)
        {
            if (players.Count >= minimumPlayersToStartResult)
            {
                matchArmed = true;
            }
            else
            {
                return;
            }
        }

        int aliveCount = 0;
        for (int i = 0; i < players.Count; i++)
        {
            if (!players[i].IsDead)
            {
                aliveCount++;
            }
        }

        if (localController.IsDead)
        {
            ShowResult(false);
            return;
        }

        if (aliveCount <= 1)
        {
            ShowResult(true);
        }
    }

    private void ShowResult(bool isWin)
    {
        resultShown = true;
        Time.timeScale = 0f;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == null)
            {
                continue;
            }

            bool isResultRoot = child.gameObject == resultRoot;
            bool hasKillFeed = keepKillFeedVisible && child.GetComponentInChildren<KillFeed>(true) != null;
            child.gameObject.SetActive(isResultRoot || hasKillFeed);
        }

        resultRoot.SetActive(true);
        winObject.SetActive(isWin);
        loseObject.SetActive(!isWin);
    }

    private void HideResultUI()
    {
        if (resultRoot != null)
        {
            resultRoot.SetActive(false);
        }

        if (winObject != null)
        {
            winObject.SetActive(false);
        }

        if (loseObject != null)
        {
            loseObject.SetActive(false);
        }
    }

    private void BindResultButtons()
    {
        if (resultRoot == null)
        {
            return;
        }

        Button[] buttons = resultRoot.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
            {
                continue;
            }

            string loweredButtonName = button.name.ToLowerInvariant();
            string loweredReturnName = returnButtonName.ToLowerInvariant();

            if (loweredButtonName == loweredReturnName || loweredButtonName.StartsWith(loweredReturnName + " ("))
            {
                button.onClick.RemoveListener(OnReturnToMenuClicked);
                button.onClick.AddListener(OnReturnToMenuClicked);
            }
            else
            {
                button.gameObject.SetActive(false);
            }
        }
    }

    private async void OnReturnToMenuClicked()
    {
        if (actionInProgress)
        {
            return;
        }

        actionInProgress = true;
        try
        {
            await LeaveRoomIfNeeded();
            RestoreGameplayTime();

            if (!string.IsNullOrWhiteSpace(menuSceneName) && Application.CanStreamedLevelBeLoaded(menuSceneName))
            {
                SceneManager.LoadScene(menuSceneName);
                return;
            }

            if (!string.IsNullOrWhiteSpace(menuSceneName))
            {
                Debug.LogWarning($"MatchResultUIController: menu scene '{menuSceneName}' could not be loaded. Falling back to menu UI reset.");
            }

            RestoreCanvasAfterResult();
            OpenMenuUI();
        }
        finally
        {
            actionInProgress = false;
        }
    }

    private static async System.Threading.Tasks.Task LeaveRoomIfNeeded()
    {
        SharedRoomSessionManager sessionManager = SharedRoomSessionManager.Instance;
        if (sessionManager == null || !sessionManager.HasActiveSession)
        {
            return;
        }

        await sessionManager.LeaveRoom();
    }

    private void RestoreCanvasAfterResult()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == null)
            {
                continue;
            }

            child.gameObject.SetActive(true);
        }

        if (resultRoot != null)
        {
            resultRoot.SetActive(false);
        }

        if (winObject != null)
        {
            winObject.SetActive(false);
        }

        if (loseObject != null)
        {
            loseObject.SetActive(false);
        }
    }

    private static void RestoreGameplayTime()
    {
        Time.timeScale = 1f;
    }

    private static void OpenMenuUI()
    {
        ButtonClick buttonClick = FindFirstObjectByType<ButtonClick>();
        if (buttonClick != null)
        {
            buttonClick.ResetToMainMenu();
        }

        LobbyUI lobbyUI = FindFirstObjectByType<LobbyUI>();
        if (lobbyUI != null)
        {
            lobbyUI.gameObject.SetActive(false);
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

    private void OnDisable()
    {
        RestoreGameplayTime();
    }

    private void OnDestroy()
    {
        RestoreGameplayTime();
    }
}
