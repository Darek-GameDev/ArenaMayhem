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
		localPlayerIndex = Mathf.Clamp(playerIndex, 0, Mathf.Max(0, currentPlayerCount - 1));
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
		localPlayerIndex = Mathf.Clamp(localPlayerIndex, 0, Mathf.Max(0, currentPlayerCount - 1));
		RefreshReadyUI();
	}

	public void SetPlayerReady(int playerIndex, bool ready)
	{
		if (!TryGetSlot(playerIndex, out PlayerReadySlot slot))
		{
			return;
		}

		slot.isReady = ready;
		RefreshReadyUI();
	}

	public bool AreAllCurrentPlayersReady()
	{
		if (playerSlots == null || currentPlayerCount <= 0)
		{
			return false;
		}

		for (int i = 0; i < currentPlayerCount; i++)
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
				SetSlotState(slot, false);
				continue;
			}

			SetSlotState(slot, slot.isReady);
		}

		UpdateStartState(AreAllCurrentPlayersReady());
	}

	private void SetSlotState(PlayerReadySlot slot, bool isReady)
	{
		SetActiveSafe(slot.xMark, !isReady);
		SetActiveSafe(slot.tickMark, isReady);
		SetActiveSafe(slot.unreadyButton, !isReady);
		SetActiveSafe(slot.readyButton, isReady);

		SetButtonInteractable(slot.xMark, !isReady);
		SetButtonInteractable(slot.tickMark, isReady);
		SetButtonInteractable(slot.unreadyButton, !isReady);
		SetButtonInteractable(slot.readyButton, isReady);

		SetCanvasGroupState(slot.xMark, !isReady);
		SetCanvasGroupState(slot.tickMark, isReady);
		SetCanvasGroupState(slot.unreadyButton, !isReady);
		SetCanvasGroupState(slot.readyButton, isReady);
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
		if (playerSlots == null || slotIndex < 0 || slotIndex >= currentPlayerCount)
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
}
