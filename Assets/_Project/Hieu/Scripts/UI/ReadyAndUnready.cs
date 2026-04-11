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
		RefreshReadyUI();
	}

	private void OnEnable()
	{
		ResetPlayersToDefaultState();
		RefreshReadyUI();
	}

	private void Update()
	{
		RefreshReadyUI();
	}

	public void OnUnreadyClicked()
	{
		SetPlayerReady(localPlayerIndex, true);
	}

	public void OnReadyClicked()
	{
		SetPlayerReady(localPlayerIndex, false);
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

			SetSlotState(slot, slot.isReady);
		}

		UpdateStartState(currentPlayerCount > 0);
	}

	private void SetSlotState(PlayerReadySlot slot, bool isReady)
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
}
