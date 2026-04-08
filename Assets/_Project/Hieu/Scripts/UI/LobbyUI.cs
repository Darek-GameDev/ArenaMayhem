using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
	private enum AvatarKind
	{
		None,
		Knight,
		Archer,
	}

	[Serializable]
	private class PlayerAvatarSlot
	{
		public GameObject slotRoot;
		public GameObject knightAvatar;
		public GameObject archerAvatar;

		[NonSerialized] public AvatarKind avatarKind = AvatarKind.None;
	}

	[Header("References")]
	[SerializeField] private GameObject menuUI;
	[SerializeField] private Button startGameButton;
	[SerializeField] private Button leaveRoomButton;
	[SerializeField] private Button removeRoomButton;

	[Header("Lobby Status")]
	[SerializeField] private TMP_Text playerCountText;
	[SerializeField] private PlayerAvatarSlot[] playerAvatarSlots;
	[SerializeField] private int maxPlayers = 4;
	[SerializeField] private int currentPlayerCount = 1;
	[SerializeField] private bool isRoomOwner = true;
	[SerializeField] private Color waitingColor = Color.white;
	[SerializeField] private Color fullColor = Color.red;

	[Header("Scene")]
	[SerializeField] private string gameSceneName;

	[Header("Rules")]
	[SerializeField] private bool autoApplyFirstSlotFromClassChoose = true;
	[SerializeField] private string defaultClassName = "Knight";
	[SerializeField] private int requiredPlayersToStart = 1;

	private void Awake()
	{
		RefreshLobbyUI();
	}

	private void OnEnable()
	{
		RefreshLobbyUI();
	}

	private void Update()
	{
		RefreshButtonVisibility();
	}

	public void OnStartGameClicked()
	{
		if (string.IsNullOrWhiteSpace(gameSceneName))
		{
			Debug.LogWarning("LobbyUI: chua gan ten scene game.");
			return;
		}

		if (currentPlayerCount < Mathf.Max(1, requiredPlayersToStart))
		{
			return;
		}

		SceneManager.LoadScene(gameSceneName);
	}

	public void OnLeaveRoomClicked()
	{
		ExitRoomToMenu();
	}

	public void OnRemoveRoomClicked()
	{
		if (!isRoomOwner)
		{
			return;
		}

		ExitRoomToMenu();
	}

	public void SetRoomOwner(bool value)
	{
		isRoomOwner = value;
		RefreshButtonVisibility();
	}

	public void SetPlayerCount(int count)
	{
		currentPlayerCount = Mathf.Clamp(count, 0, GetMaxPlayers());
		RefreshLobbyUI();
	}

	public void SetLocalPlayerClass(string className)
	{
		SetSlotClass(0, className);
	}

	public void SetSlotClass(int slotIndex, string className)
	{
		if (!TryGetSlot(slotIndex, out PlayerAvatarSlot slot))
		{
			return;
		}

		slot.avatarKind = GetAvatarKind(className);
		RefreshLobbyUI();
	}

	public void ClearSlot(int slotIndex)
	{
		if (!TryGetSlot(slotIndex, out PlayerAvatarSlot slot))
		{
			return;
		}

		slot.avatarKind = AvatarKind.None;
		RefreshLobbyUI();
	}

	public void RefreshLobbyUI()
	{
		ApplyDefaultLocalSlotIfNeeded();
		int resolvedMaxPlayers = GetMaxPlayers();
		int displayedPlayerCount = Mathf.Clamp(currentPlayerCount, 0, resolvedMaxPlayers);

		for (int i = 0; i < playerAvatarSlots.Length; i++)
		{
			PlayerAvatarSlot slot = playerAvatarSlots[i];
			if (slot == null)
			{
				continue;
			}

			bool occupied = i < displayedPlayerCount;
			SetSlotVisible(slot, occupied);

			if (occupied)
			{
				ApplyAvatarKindToSlot(slot, slot.avatarKind);
			}
		}

		UpdatePlayerCountText(displayedPlayerCount, resolvedMaxPlayers);
		RefreshButtonVisibility();
	}

	private void RefreshButtonVisibility()
	{
		if (leaveRoomButton != null)
		{
			leaveRoomButton.gameObject.SetActive(true);
		}

		if (removeRoomButton != null)
		{
			removeRoomButton.gameObject.SetActive(isRoomOwner);
		}

		if (startGameButton != null)
		{
			startGameButton.interactable = currentPlayerCount >= Mathf.Max(1, requiredPlayersToStart);
		}
	}

	private void UpdatePlayerCountText(int filledSlotCount, int resolvedMaxPlayers)
	{
		if (playerCountText == null)
		{
			return;
		}

		playerCountText.text = $"{filledSlotCount}/{resolvedMaxPlayers}";
		playerCountText.color = filledSlotCount >= resolvedMaxPlayers ? fullColor : waitingColor;
	}

	private void ApplyDefaultLocalSlotIfNeeded()
	{
		if (!autoApplyFirstSlotFromClassChoose || playerAvatarSlots == null || playerAvatarSlots.Length == 0)
		{
			return;
		}

		PlayerAvatarSlot firstSlot = playerAvatarSlots[0];
		if (firstSlot == null)
		{
			return;
		}

		if (firstSlot.avatarKind != AvatarKind.None)
		{
			return;
		}

		firstSlot.avatarKind = GetAvatarKind(ClassChoose.LastConfirmedClassName);
		if (firstSlot.avatarKind == AvatarKind.None)
		{
			firstSlot.avatarKind = GetAvatarKind(defaultClassName);
		}
	}

	private void ApplyAvatarKindToSlot(PlayerAvatarSlot slot, AvatarKind avatarKind)
	{
		if (slot == null)
		{
			return;
		}

		if (slot.knightAvatar != null)
		{
			slot.knightAvatar.SetActive(avatarKind == AvatarKind.Knight);
		}

		if (slot.archerAvatar != null)
		{
			slot.archerAvatar.SetActive(avatarKind == AvatarKind.Archer);
		}
	}

	private void SetSlotVisible(PlayerAvatarSlot slot, bool visible)
	{
		if (slot == null)
		{
			return;
		}

		if (slot.slotRoot != null)
		{
			slot.slotRoot.SetActive(visible);
		}

		if (!visible)
		{
			if (slot.knightAvatar != null)
			{
				slot.knightAvatar.SetActive(false);
			}

			if (slot.archerAvatar != null)
			{
				slot.archerAvatar.SetActive(false);
			}
		}
	}

	private AvatarKind GetAvatarKind(string className)
	{
		if (string.IsNullOrWhiteSpace(className))
		{
			return AvatarKind.None;
		}

		switch (className.Trim())
		{
			case "Knight":
				return AvatarKind.Knight;
			case "Archer":
				return AvatarKind.Archer;
			default:
				return AvatarKind.None;
		}
	}

	private void ExitRoomToMenu()
	{
		if (gameObject != null)
		{
			gameObject.SetActive(false);
		}

		if (menuUI != null)
		{
			menuUI.SetActive(true);
		}
	}

	private int GetMaxPlayers()
	{
		if (maxPlayers > 0)
		{
			return maxPlayers;
		}

		return playerAvatarSlots != null && playerAvatarSlots.Length > 0 ? playerAvatarSlots.Length : 1;
	}

	private bool TryGetSlot(int slotIndex, out PlayerAvatarSlot slot)
	{
		slot = null;
		if (playerAvatarSlots == null || slotIndex < 0 || slotIndex >= playerAvatarSlots.Length)
		{
			return false;
		}

		slot = playerAvatarSlots[slotIndex];
		return slot != null;
	}
}
