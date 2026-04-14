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
	[SerializeField] private bool hideLobbyOnSceneStart = true;
	[SerializeField] private bool enableLegacyAvatarSync = false;
	[SerializeField] private bool hideLegacyReadyIconsInSlots = false;
	[SerializeField] private bool cloneRoleImagesFromFirstSlot = true;

	private void Awake()
	{
		if (enableLegacyAvatarSync)
		{
			ResolveSlotAvatarReferences();
			CloneMissingRoleImagesFromFirstSlot();
			ResolveSlotAvatarReferences();
		}
		RefreshLobbyUI();
	}

	private void Start()
	{
		if (!hideLobbyOnSceneStart)
		{
			return;
		}

		if (gameObject != null)
		{
			gameObject.SetActive(false);
		}
	}

	private void OnEnable()
	{
		if (enableLegacyAvatarSync)
		{
			ResolveSlotAvatarReferences();
			CloneMissingRoleImagesFromFirstSlot();
			ResolveSlotAvatarReferences();
		}
		RefreshLobbyUI();
	}

	private void Update()
	{
		SyncLobbyFromSession();
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

		SharedRoomSessionManager sessionManager = SharedRoomSessionManager.Instance;
		if (sessionManager != null && sessionManager.HasActiveSession)
		{
			sessionManager.StartGameIfReady();
			return;
		}

		if (gameObject != null)
		{
			gameObject.SetActive(false);
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

	public void ResetLobbyUI()
	{
		currentPlayerCount = 1;
		isRoomOwner = true;

		if (playerAvatarSlots != null)
		{
			for (int i = 0; i < playerAvatarSlots.Length; i++)
			{
				if (playerAvatarSlots[i] != null)
				{
					playerAvatarSlots[i].avatarKind = AvatarKind.None;
				}
			}
		}

		RefreshLobbyUI();
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
		if (!enableLegacyAvatarSync)
		{
			return;
		}

		SetSlotClass(0, className);
	}

	public void SetSlotClass(int slotIndex, string className)
	{
		if (!enableLegacyAvatarSync)
		{
			return;
		}

		if (!TryGetSlot(slotIndex, out PlayerAvatarSlot slot))
		{
			return;
		}

		slot.avatarKind = GetAvatarKind(className);
		RefreshLobbyUI();
	}

	public void ClearSlot(int slotIndex)
	{
		if (!enableLegacyAvatarSync)
		{
			return;
		}

		if (!TryGetSlot(slotIndex, out PlayerAvatarSlot slot))
		{
			return;
		}

		slot.avatarKind = AvatarKind.None;
		RefreshLobbyUI();
	}

	public void RefreshLobbyUI()
	{
		int resolvedMaxPlayers = GetMaxPlayers();
		int displayedPlayerCount = Mathf.Clamp(currentPlayerCount, 0, resolvedMaxPlayers);

		if (enableLegacyAvatarSync && playerAvatarSlots != null)
		{
			ApplyDefaultLocalSlotIfNeeded();

			for (int i = 0; i < playerAvatarSlots.Length; i++)
			{
				PlayerAvatarSlot slot = playerAvatarSlots[i];
				if (slot == null)
				{
					continue;
				}

				HideLegacyReadyIcons(slot);

				bool occupied = i < displayedPlayerCount;
				SetSlotVisible(slot, occupied);

				if (occupied)
				{
					ApplyAvatarKindToSlot(slot, slot.avatarKind);
				}
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
			startGameButton.interactable = isRoomOwner && currentPlayerCount >= Mathf.Max(1, requiredPlayersToStart);
		}
	}

	private void ResolveSlotAvatarReferences()
	{
		if (playerAvatarSlots == null)
		{
			return;
		}

		for (int i = 0; i < playerAvatarSlots.Length; i++)
		{
			PlayerAvatarSlot slot = playerAvatarSlots[i];
			if (slot == null)
			{
				continue;
			}

			if (slot.slotRoot == null)
			{
				if (slot.knightAvatar != null)
				{
					slot.slotRoot = slot.knightAvatar.transform.parent != null ? slot.knightAvatar.transform.parent.gameObject : slot.knightAvatar;
				}
				else if (slot.archerAvatar != null)
				{
					slot.slotRoot = slot.archerAvatar.transform.parent != null ? slot.archerAvatar.transform.parent.gameObject : slot.archerAvatar;
				}
			}

			if (slot.slotRoot == null)
			{
				continue;
			}

			if (slot.knightAvatar == null)
			{
				slot.knightAvatar = FindChildByKeyword(slot.slotRoot.transform, "knight");
			}

			if (slot.archerAvatar == null)
			{
				slot.archerAvatar = FindChildByKeyword(slot.slotRoot.transform, "archer");
			}
		}
	}

	private static GameObject FindChildByKeyword(Transform root, string keyword)
	{
		if (root == null || string.IsNullOrEmpty(keyword))
		{
			return null;
		}

		string loweredKeyword = keyword.ToLowerInvariant();
		Transform[] children = root.GetComponentsInChildren<Transform>(true);
		for (int i = 0; i < children.Length; i++)
		{
			Transform child = children[i];
			if (child == null || child == root)
			{
				continue;
			}

			if (child.name.ToLowerInvariant().Contains(loweredKeyword))
			{
				return child.gameObject;
			}
		}

		return null;
	}

	private void CloneMissingRoleImagesFromFirstSlot()
	{
		if (!cloneRoleImagesFromFirstSlot || playerAvatarSlots == null || playerAvatarSlots.Length == 0)
		{
			return;
		}

		PlayerAvatarSlot templateSlot = playerAvatarSlots[0];
		if (templateSlot == null || templateSlot.slotRoot == null)
		{
			return;
		}

		GameObject templateKnight = templateSlot.knightAvatar != null ? templateSlot.knightAvatar : FindChildByKeyword(templateSlot.slotRoot.transform, "knight");
		GameObject templateArcher = templateSlot.archerAvatar != null ? templateSlot.archerAvatar : FindChildByKeyword(templateSlot.slotRoot.transform, "archer");

		for (int i = 1; i < playerAvatarSlots.Length; i++)
		{
			PlayerAvatarSlot slot = playerAvatarSlots[i];
			if (slot == null || slot.slotRoot == null)
			{
				continue;
			}

			if (slot.knightAvatar == null && templateKnight != null)
			{
				slot.knightAvatar = Instantiate(templateKnight, slot.slotRoot.transform, false);
				slot.knightAvatar.name = $"P{i + 1}_Knight";
				slot.knightAvatar.SetActive(false);
			}

			if (slot.archerAvatar == null && templateArcher != null)
			{
				slot.archerAvatar = Instantiate(templateArcher, slot.slotRoot.transform, false);
				slot.archerAvatar.name = $"P{i + 1}_Archer";
				slot.archerAvatar.SetActive(false);
			}
		}
	}

	private void SyncLobbyFromSession()
	{
		SharedRoomSessionManager sessionManager = SharedRoomSessionManager.Instance;
		if (sessionManager == null || !sessionManager.HasActiveSession)
		{
			return;
		}

		var players = sessionManager.GetPlayersOrderedById();
		currentPlayerCount = Mathf.Clamp(players.Count, 0, GetMaxPlayers());

		if (sessionManager.Runner != null && sessionManager.Runner.LocalPlayer.IsRealPlayer)
		{
			isRoomOwner = sessionManager.IsRoomOwner(sessionManager.Runner.LocalPlayer);
		}

		if (enableLegacyAvatarSync && playerAvatarSlots != null)
		{
			for (int i = 0; i < playerAvatarSlots.Length; i++)
			{
				PlayerAvatarSlot slot = playerAvatarSlots[i];
				if (slot == null)
				{
					continue;
				}

				if (i >= players.Count)
				{
					slot.avatarKind = AvatarKind.None;
					continue;
				}

				SharedPlayerClassType classType = SharedPlayerClassType.Unknown;
				if (sessionManager.Runner != null)
				{
					SharedRoomSessionManager.TryGetPlayerClass(sessionManager.Runner, players[i], out classType);
				}

				if (classType == SharedPlayerClassType.Unknown)
				{
					classType = sessionManager.GetPlayerClass(players[i], SharedPlayerClassType.Unknown);
				}

				AvatarKind resolvedAvatar = ToAvatarKind(classType);
				if (resolvedAvatar != AvatarKind.None)
				{
					slot.avatarKind = resolvedAvatar;
				}
				else
				{
					slot.avatarKind = AvatarKind.None;
				}
			}
		}

		RefreshLobbyUI();
	}

	private static AvatarKind ToAvatarKind(SharedPlayerClassType classType)
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

	private void UpdatePlayerCountText(int filledSlotCount, int resolvedMaxPlayers)
	{
		if (playerCountText == null)
		{
			return;
		}

		playerCountText.text = $"PLAYER IN ROOM: {filledSlotCount}/{resolvedMaxPlayers}";
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

	private void HideLegacyReadyIcons(PlayerAvatarSlot slot)
	{
		if (!hideLegacyReadyIconsInSlots || slot == null || slot.slotRoot == null)
		{
			return;
		}

		Transform[] children = slot.slotRoot.GetComponentsInChildren<Transform>(true);
		for (int i = 0; i < children.Length; i++)
		{
			Transform child = children[i];
			if (child == null || child == slot.slotRoot.transform)
			{
				continue;
			}

			string loweredName = child.name.ToLowerInvariant();
			bool isReadyVisual = loweredName.Contains("tick") || loweredName.Contains("ready") || loweredName.Contains("xmark") || loweredName.Contains("check") || loweredName == "x";
			if (!isReadyVisual)
			{
				continue;
			}

			if (slot.knightAvatar != null && child == slot.knightAvatar.transform)
			{
				continue;
			}

			if (slot.archerAvatar != null && child == slot.archerAvatar.transform)
			{
				continue;
			}

			child.gameObject.SetActive(false);
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
