using UnityEngine;
using UnityEngine.UI;

public class ReadyAndUnready : MonoBehaviour
{
	[System.Serializable]
	private class PlayerReadySlot
	{
		public GameObject slotRoot;
		public GameObject xMark;
		public GameObject tickMark;
		public GameObject readyButton;
		public GameObject unreadyButton;

		[HideInInspector] public bool isReady;
	}

	[Header("References")]
	[SerializeField] private Button startGameButton;
	[SerializeField] private GameObject lobbyUI;

	[Header("Players")]
	[SerializeField] private PlayerReadySlot[] playerSlots;
	[SerializeField] private int currentPlayerCount = 1;
	[SerializeField] private int localPlayerIndex;

	private void Awake()
	{
		ResetPlayersToDefaultState();
		DisableAllReadyVisuals();
		RefreshReadyUI();
	}

	private void OnEnable()
	{
		ResetPlayersToDefaultState();
		DisableAllReadyVisuals();
		RefreshReadyUI();
	}

	private void Update()
	{
		SyncReadyStateFromSession();
		DisableAllReadyVisuals();
		RefreshReadyUI();
	}

	public void OnUnreadyClicked()
	{
		// Ready flow is disabled for this lobby layout.
	}

	public void OnReadyClicked()
	{
		// Ready flow is disabled for this lobby layout.
	}

	public void SetLocalPlayerIndex(int playerIndex)
	{
		localPlayerIndex = Mathf.Clamp(playerIndex, 0, Mathf.Max(0, GetResolvedPlayerCount() - 1));
	}

	public void SetPlayerCount(int count)
	{
		if (playerSlots == null)
		{
			currentPlayerCount = 0;
			RefreshReadyUI();
			return;
		}

		currentPlayerCount = Mathf.Clamp(count, 0, playerSlots.Length);
		localPlayerIndex = Mathf.Clamp(localPlayerIndex, 0, Mathf.Max(0, GetResolvedPlayerCount() - 1));
		RefreshReadyUI();
	}

	public void SetPlayerReady(int playerIndex, bool ready)
	{
		if (!TryGetSlot(playerIndex, out PlayerReadySlot slot))
		{
			return;
		}

		slot.isReady = ready;

		if (playerIndex == localPlayerIndex)
		{
			SharedRoomSessionManager sessionManager = SharedRoomSessionManager.Instance;
			if (sessionManager != null)
			{
				sessionManager.SetLocalReady(ready);
			}
		}

		RefreshReadyUI();
	}

	public bool AreAllCurrentPlayersReady()
	{
		int resolvedPlayerCount = GetResolvedPlayerCount();
		if (playerSlots == null || resolvedPlayerCount <= 0)
		{
			return false;
		}

		for (int i = 0; i < resolvedPlayerCount; i++)
		{
			if (playerSlots[i] == null || !playerSlots[i].isReady)
			{
				return false;
			}
		}

		return true;
	}

	public void RefreshReadyUI()
	{
		currentPlayerCount = GetResolvedPlayerCount();

		if (playerSlots == null)
		{
			UpdateStartState(false);
			return;
		}

		for (int i = 0; i < playerSlots.Length; i++)
		{
			PlayerReadySlot slot = playerSlots[i];
			if (slot == null)
			{
				continue;
			}

			bool hasPlayer = i < currentPlayerCount;
			SetActiveSafe(slot.slotRoot, hasPlayer);

			if (!hasPlayer)
			{
				SetSlotEmptyState(slot);
				continue;
			}

			SetSlotState(slot, slot.isReady, i == localPlayerIndex);
		}

		UpdateStartState(currentPlayerCount > 0);
	}

	private void SetSlotState(PlayerReadySlot slot, bool isReady, bool isLocalSlot)
	{
		bool showX = false;
		bool showTick = false;
		bool showReadyButton = false;
		bool showUnreadyButton = false;

		SetActiveSafe(slot.xMark, showX);
		SetActiveSafe(slot.tickMark, showTick);
		SetActiveSafe(slot.unreadyButton, showUnreadyButton);
		SetActiveSafe(slot.readyButton, showReadyButton);

		SetButtonInteractable(slot.xMark, false);
		SetButtonInteractable(slot.tickMark, false);
		SetButtonInteractable(slot.unreadyButton, showUnreadyButton);
		SetButtonInteractable(slot.readyButton, showReadyButton);

		SetCanvasGroupState(slot.xMark, showX);
		SetCanvasGroupState(slot.tickMark, showTick);
		SetCanvasGroupState(slot.unreadyButton, showUnreadyButton);
		SetCanvasGroupState(slot.readyButton, showReadyButton);
	}

	private void SetSlotEmptyState(PlayerReadySlot slot)
	{
		SetActiveSafe(slot.xMark, false);
		SetActiveSafe(slot.tickMark, false);
		SetActiveSafe(slot.unreadyButton, false);
		SetActiveSafe(slot.readyButton, false);

		SetButtonInteractable(slot.xMark, false);
		SetButtonInteractable(slot.tickMark, false);
		SetButtonInteractable(slot.unreadyButton, false);
		SetButtonInteractable(slot.readyButton, false);

		SetCanvasGroupState(slot.xMark, false);
		SetCanvasGroupState(slot.tickMark, false);
		SetCanvasGroupState(slot.unreadyButton, false);
		SetCanvasGroupState(slot.readyButton, false);
	}

	private void UpdateStartState(bool canStart)
	{
		if (startGameButton != null)
		{
			startGameButton.interactable = canStart;
		}
	}

	private void ResetPlayersToDefaultState()
	{
		if (playerSlots == null)
		{
			return;
		}

		for (int i = 0; i < playerSlots.Length; i++)
		{
			PlayerReadySlot slot = playerSlots[i];
			if (slot != null)
			{
				slot.isReady = false;
			}
		}
	}

	private bool TryGetSlot(int slotIndex, out PlayerReadySlot slot)
	{
		slot = null;
		int resolvedPlayerCount = GetResolvedPlayerCount();
		if (playerSlots == null || slotIndex < 0 || slotIndex >= resolvedPlayerCount)
		{
			return false;
		}

		slot = playerSlots[slotIndex];
		return slot != null;
	}

	private void SetActiveSafe(GameObject target, bool visible)
	{
		if (target != null)
		{
			target.SetActive(visible);
		}
	}

	private void SetButtonInteractable(GameObject target, bool visible)
	{
		if (target == null)
		{
			return;
		}

		Button button = target.GetComponent<Button>();
		if (button != null)
		{
			button.interactable = visible;
		}
	}

	private void SetCanvasGroupState(GameObject target, bool visible)
	{
		if (target == null)
		{
			return;
		}

		CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
		if (canvasGroup != null)
		{
			canvasGroup.alpha = visible ? 1f : 0f;
			canvasGroup.interactable = visible;
			canvasGroup.blocksRaycasts = visible;
		}
	}

	private int GetResolvedPlayerCount()
	{
		SharedRoomSessionManager sessionManager = SharedRoomSessionManager.Instance;
		if (sessionManager != null && sessionManager.HasActiveSession)
		{
			int joinedCount = sessionManager.GetJoinedPlayerCount();
			if (playerSlots != null)
			{
				return Mathf.Clamp(joinedCount, 0, playerSlots.Length);
			}

			return Mathf.Max(0, joinedCount);
		}

		if (playerSlots != null)
		{
			return Mathf.Clamp(1, 0, playerSlots.Length);
		}

		return 1;
	}

	private void SyncReadyStateFromSession()
	{
		SharedRoomSessionManager sessionManager = SharedRoomSessionManager.Instance;
		if (sessionManager == null || !sessionManager.HasActiveSession || playerSlots == null)
		{
			return;
		}

		var players = sessionManager.GetPlayersOrderedById();
		currentPlayerCount = Mathf.Clamp(players.Count, 0, playerSlots.Length);

		if (sessionManager.Runner != null && sessionManager.Runner.LocalPlayer.IsRealPlayer)
		{
			for (int i = 0; i < players.Count; i++)
			{
				if (players[i] == sessionManager.Runner.LocalPlayer)
				{
					localPlayerIndex = i;
					break;
				}
			}
		}

		for (int i = 0; i < playerSlots.Length; i++)
		{
			if (playerSlots[i] == null)
			{
				continue;
			}

			if (i >= players.Count)
			{
				playerSlots[i].isReady = false;
				continue;
			}

			playerSlots[i].isReady = sessionManager.IsPlayerReady(players[i]);
		}
	}

	private void DisableAllReadyVisuals()
	{
		if (playerSlots == null)
		{
			return;
		}

		for (int i = 0; i < playerSlots.Length; i++)
		{
			PlayerReadySlot slot = playerSlots[i];
			if (slot == null)
			{
				continue;
			}

			SetActiveSafe(slot.xMark, false);
			SetActiveSafe(slot.tickMark, false);
			SetActiveSafe(slot.readyButton, false);
			SetActiveSafe(slot.unreadyButton, false);

			HideReadyNamedChildren(slot.slotRoot);
		}
	}

	private void HideReadyNamedChildren(GameObject root)
	{
		if (root == null)
		{
			return;
		}

		Transform[] children = root.GetComponentsInChildren<Transform>(true);
		for (int i = 0; i < children.Length; i++)
		{
			Transform child = children[i];
			if (child == null || child == root.transform)
			{
				continue;
			}

			string loweredName = child.name.ToLowerInvariant();
			bool isReadyVisual = loweredName.Contains("tick") || loweredName.Contains("ready") || loweredName.Contains("xmark") || loweredName.Contains("check") || loweredName == "x";
			if (isReadyVisual)
			{
				child.gameObject.SetActive(false);
			}
		}
	}
}
