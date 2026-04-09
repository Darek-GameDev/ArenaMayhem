using System;
using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
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
	[SerializeField] private Button readyButton;
	[SerializeField] private TMP_Text readyButtonTmpText;
	[SerializeField] private Text readyButtonLegacyText;
	[SerializeField] private TMP_Text lobbyStatusTmpText;
	[SerializeField] private Text lobbyStatusLegacyText;

	[Header("Lobby Status")]
	[SerializeField] private TMP_Text playerCountText;
	[SerializeField] private PlayerAvatarSlot[] playerAvatarSlots;
	[SerializeField] private int maxPlayers = 6;
	[SerializeField] private int offlinePlayerCount = 1;
	[SerializeField] private bool offlineRoomOwner = true;
	[SerializeField] private Color waitingColor = Color.white;
	[SerializeField] private Color fullColor = Color.red;

	[Header("Rules")]
	[SerializeField] private bool autoApplyFirstSlotFromClassChoose = true;
	[SerializeField] private string defaultClassName = "Knight";

	private SharedRoomSessionManager sessionManager;

	private void Awake()
	{
		RefreshLobbyUI();
	}

	private void OnEnable()
	{
		ResolveSessionManager();
		RefreshLobbyUI();
	}

	private void Update()
	{
		RefreshLobbyUI();
	}

	public void OnStartGameClicked()
	{
		ResolveSessionManager();
		if (sessionManager == null)
		{
			SetLobbyStatus("Chua co Session Manager.");
			return;
		}

		if (!sessionManager.StartGameIfReady())
		{
			if (sessionManager.CanStartGame(out string reason))
			{
				SetLobbyStatus("Khong the bat dau tran.");
			}
			else
			{
				SetLobbyStatus(reason);
			}
		}
	}

	public void OnLeaveRoomClicked()
	{
		ResolveSessionManager();
		if (sessionManager != null)
		{
			sessionManager.LeaveRoom();
		}

		ExitRoomToMenu();
	}

	public void OnRemoveRoomClicked()
	{
		ResolveSessionManager();
		if (sessionManager != null && !sessionManager.IsLocalPlayerOwner())
		{
			SetLobbyStatus("Chi owner moi duoc remove room.");
			return;
		}

		if (sessionManager != null)
		{
			sessionManager.LeaveRoom();
		}

		ExitRoomToMenu();
	}

	public void OnReadyClicked()
	{
		ResolveSessionManager();
		if (sessionManager == null || !sessionManager.HasActiveSession)
		{
			return;
		}

		bool nextReady = !sessionManager.IsLocalPlayerReady();
		sessionManager.SetLocalReady(nextReady);
		RefreshLobbyUI();
	}

	public void SetRoomOwner(bool value)
	{
		offlineRoomOwner = value;
		RefreshLobbyUI();
	}

	public void SetPlayerCount(int count)
	{
		offlinePlayerCount = Mathf.Clamp(count, 0, GetResolvedMaxPlayers());
		RefreshLobbyUI();
	}

	public void SetLocalPlayerClass(string className)
	{
		ResolveSessionManager();
		if (sessionManager != null)
		{
			sessionManager.SetLocalClass(className);
			sessionManager.SetLocalReady(true);
		}

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
		ResolveSessionManager();
		ApplyDefaultLocalSlotIfNeeded();

		int resolvedMaxPlayers = GetResolvedMaxPlayers();
		int displayedPlayerCount;
		bool isRoomOwner;
		bool localReady;
		bool canStart;
		string statusMessage;

		if (sessionManager != null && sessionManager.HasActiveSession)
		{
			displayedPlayerCount = Mathf.Clamp(sessionManager.GetJoinedPlayerCount(), 0, resolvedMaxPlayers);
			isRoomOwner = sessionManager.IsLocalPlayerOwner();
			localReady = sessionManager.IsLocalPlayerReady();
			canStart = sessionManager.CanStartGame(out statusMessage);
			ApplySlotsFromSession(displayedPlayerCount);
		}
		else
		{
			displayedPlayerCount = Mathf.Clamp(offlinePlayerCount, 0, resolvedMaxPlayers);
			isRoomOwner = offlineRoomOwner;
			localReady = true;
			canStart = displayedPlayerCount >= 1;
			statusMessage = string.Empty;
			ApplyOfflineSlots(displayedPlayerCount);
		}

		UpdatePlayerCountText(displayedPlayerCount, resolvedMaxPlayers);
		RefreshButtonVisibility(isRoomOwner, canStart);
		RefreshReadyButton(localReady);
		SetLobbyStatus(statusMessage);
	}

	private void ResolveSessionManager()
	{
		if (sessionManager == null)
		{
			sessionManager = SharedRoomSessionManager.Instance;
		}
	}

	private void ApplySlotsFromSession(int displayedPlayerCount)
	{
		IReadOnlyList<PlayerRef> players = sessionManager.GetPlayersOrderedById();
		for (int i = 0; i < playerAvatarSlots.Length; i++)
		{
			PlayerAvatarSlot slot = playerAvatarSlots[i];
			if (slot == null)
			{
				continue;
			}

			bool occupied = i < displayedPlayerCount;
			SetSlotVisible(slot, occupied);
			if (!occupied)
			{
				continue;
			}

			AvatarKind avatarKind = slot.avatarKind;
			if (i < players.Count)
			{
				SharedPlayerClassType classType = sessionManager.GetPlayerClass(players[i], SharedPlayerClassType.Unknown);
				avatarKind = GetAvatarKind(classType);
				if (avatarKind == AvatarKind.None)
				{
					avatarKind = slot.avatarKind != AvatarKind.None ? slot.avatarKind : GetAvatarKind(defaultClassName);
				}
			}

			slot.avatarKind = avatarKind;

			ApplyAvatarKindToSlot(slot, avatarKind);
		}
	}

	private void ApplyOfflineSlots(int displayedPlayerCount)
	{
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
	}

	private void RefreshButtonVisibility(bool isRoomOwner, bool canStart)
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
			startGameButton.gameObject.SetActive(isRoomOwner);
			startGameButton.interactable = isRoomOwner && canStart;
		}
	}

	private void RefreshReadyButton(bool localReady)
	{
		if (readyButton != null)
		{
			readyButton.interactable = sessionManager != null && sessionManager.HasActiveSession;
		}

		string readyLabel = localReady ? "UNREADY" : "READY";
		if (readyButtonTmpText != null)
		{
			readyButtonTmpText.text = readyLabel;
		}

		if (readyButtonLegacyText != null)
		{
			readyButtonLegacyText.text = readyLabel;
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

	private void SetLobbyStatus(string message)
	{
		if (string.IsNullOrWhiteSpace(message))
		{
			message = string.Empty;
		}

		if (lobbyStatusTmpText != null)
		{
			lobbyStatusTmpText.text = message;
		}

		if (lobbyStatusLegacyText != null)
		{
			lobbyStatusLegacyText.text = message;
		}
	}

	private void ApplyDefaultLocalSlotIfNeeded()
	{
		if (!autoApplyFirstSlotFromClassChoose || playerAvatarSlots == null || playerAvatarSlots.Length == 0)
		{
			return;
		}

		PlayerAvatarSlot firstSlot = playerAvatarSlots[0];
		if (firstSlot == null || firstSlot.avatarKind != AvatarKind.None)
		{
			return;
		}

		firstSlot.avatarKind = GetAvatarKind(ClassChoose.LastConfirmedClassName);
		if (firstSlot.avatarKind == AvatarKind.None)
		{
			firstSlot.avatarKind = GetAvatarKind(defaultClassName);
		}
	}

	private static void ApplyAvatarKindToSlot(PlayerAvatarSlot slot, AvatarKind avatarKind)
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

	private static void SetSlotVisible(PlayerAvatarSlot slot, bool visible)
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
		return GetAvatarKind(SharedPlayerClassTypeUtility.FromClassName(className));
	}

	private static AvatarKind GetAvatarKind(SharedPlayerClassType classType)
	{
		switch (classType)
		{
			case SharedPlayerClassType.Knight:
				return AvatarKind.Knight;
			case SharedPlayerClassType.Archer:
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

	private int GetResolvedMaxPlayers()
	{
		if (sessionManager != null)
		{
			return sessionManager.MaxPlayersPerRoom;
		}

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
