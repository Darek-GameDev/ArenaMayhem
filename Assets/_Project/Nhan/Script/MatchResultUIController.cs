using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MatchResultUIController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool enableSystem = true;
    [SerializeField] private int maxPlayers = 6;
    [SerializeField] [Min(2)] private int minimumPlayersToStartResult = 2;

    [Header("Result UI Names")]
    [SerializeField] private string resultRootName = "Win_Lose_Die";
    [SerializeField] private string resultRootFallbackName = "Win/Lose/Die";
    [SerializeField] private string winObjectName = "VIctory";
    [SerializeField] private string loseObjectName = "Lose";
    [SerializeField] private string returnButtonName = "Return To Menu";

    [Header("Button Actions")]
    [SerializeField] private bool autoBindResultButtons = true;

    private SharedModePlayerController localController;
    private GameObject resultRoot;
    private GameObject winObject;
    private GameObject loseObject;
    private bool resultShown;
    private bool matchArmed;
    private bool actionInProgress;

    private void Awake()
    {
        ResolveResultObjects();

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
        ResolveResultObjects();
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

        int localRank = GetRank(players, localController);
        if (localRank <= 0)
        {
            return;
        }

        int aliveCount = 0;
        for (int i = 0; i < players.Count; i++)
        {
            if (!players[i].IsDead)
            {
                aliveCount++;
            }
        }

        bool matchFinished = aliveCount <= 1 || localController.IsDead;
        if (!matchFinished)
        {
            return;
        }

        ShowResult(localRank == 1);
    }

    private void ShowResult(bool isWin)
    {
        resultShown = true;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == null)
            {
                continue;
            }

            child.gameObject.SetActive(child.gameObject == resultRoot);
        }

        resultRoot.SetActive(true);
        winObject.SetActive(isWin);
        loseObject.SetActive(!isWin);
    }

    private void ResolveResultObjects()
    {
        if (resultRoot != null && winObject != null && loseObject != null)
        {
            return;
        }

        resultRoot = FindChildByName(transform, resultRootName);
        if (resultRoot == null)
        {
            resultRoot = FindChildByName(transform, resultRootFallbackName);
        }

        if (resultRoot == null)
        {
            return;
        }

        winObject = FindChildByName(resultRoot.transform, winObjectName);
        loseObject = FindChildByName(resultRoot.transform, loseObjectName);
    }

    private void BindResultButtons()
    {
        ResolveResultObjects();
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
        await LeaveRoomIfNeeded();
        RestoreCanvasAfterResult();
        OpenMenuUI();
        actionInProgress = false;
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

    private static int GetRank(List<SharedModePlayerController> players, SharedModePlayerController local)
    {
        if (local == null)
        {
            return -1;
        }

        players.Sort((a, b) =>
        {
            int killCompare = b.KillCount.CompareTo(a.KillCount);
            if (killCompare != 0)
            {
                return killCompare;
            }

            if (a.Object == null || b.Object == null)
            {
                return 0;
            }

            return a.Object.InputAuthority.RawEncoded.CompareTo(b.Object.InputAuthority.RawEncoded);
        });

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] == local)
            {
                return i + 1;
            }
        }

        return -1;
    }

    private static GameObject FindChildByName(Transform root, string objectName)
    {
        if (root == null || string.IsNullOrWhiteSpace(objectName))
        {
            return null;
        }

        Transform[] allChildren = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < allChildren.Length; i++)
        {
            Transform child = allChildren[i];
            if (child != null && child.name == objectName)
            {
                return child.gameObject;
            }
        }

        return null;
    }
}
