using System;
using System.Collections.Generic;
using Fusion;
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
		public TMP_Text playerNameTmpText;
		public TMP_Text classNameTmpText;

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
	[SerializeField] private int currentPlayerCount = 0;
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
	[SerializeField] private bool enableLegacyAvatarSync = true;  // Changed to true for consistent syncing
	[SerializeField] private bool hideLegacyReadyIconsInSlots = false;
	[SerializeField] private string emptyPlayerName = "PLAYER NAME";
	[SerializeField] private string emptyClassName = "CLASS NAME";

	private void Awake()
	{
		EnsurePlayerSlotsConfigured();
		
		// Always resolve slot avatar references first, regardless of enableLegacyAvatarSync
		ResolveSlotAvatarReferences();

		SyncLobbyFromSession();
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
		EnsurePlayerSlotsConfigured();
		ResolveSlotAvatarReferences();
		currentPlayerCount = 0;
		SyncLobbyFromSession();
		RefreshLobbyUI();
		
		// Subscribe to session property changes to update UI immediately
		SharedRoomSessionManager.OnSessionPropertiesChanged += OnSessionPropertiesChanged;
	}

	private void OnDisable()
	{
		// Unsubscribe from session property changes
		SharedRoomSessionManager.OnSessionPropertiesChanged -= OnSessionPropertiesChanged;
	}

	private void OnSessionPropertiesChanged()
	{
		// When session properties change, sync and refresh lobby UI immediately
		SyncLobbyFromSession();
		RefreshLobbyUI();
	}

	private void EnsurePlayerSlotsConfigured()
	{
		if (playerAvatarSlots == null || playerAvatarSlots.Length == 0)
		{
			AutoDiscoverPlayerSlots();
			return;
		}

		bool hasMissingRoot = false;
		for (int i = 0; i < playerAvatarSlots.Length; i++)
		{
			if (playerAvatarSlots[i] == null || playerAvatarSlots[i].slotRoot == null)
			{
				hasMissingRoot = true;
				break;
			}
		}

		if (!hasMissingRoot)
		{
			return;
		}

		AutoDiscoverPlayerSlots();
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

	public async void OnLeaveRoomClicked()
	{
		SharedRoomSessionManager sessionManager = SharedRoomSessionManager.Instance;
		if (sessionManager != null)
		{
			await sessionManager.LeaveRoom();
		}

		ExitRoomToMenu();
	}

	public async void OnRemoveRoomClicked()
	{
		if (!isRoomOwner)
		{
			return;
		}

		SharedRoomSessionManager sessionManager = SharedRoomSessionManager.Instance;
		if (sessionManager != null)
		{
			await sessionManager.RemoveRoomAndKickAll();
		}

		ResetLobbyUI();
		ExitRoomToMenu();
	}

	public void ResetLobbyUI()
	{
		currentPlayerCount = 0;
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
		// Update first slot display immediately when class is chosen.
		if (playerAvatarSlots == null || playerAvatarSlots.Length == 0)
		{
			return;
		}

		PlayerAvatarSlot firstSlot = playerAvatarSlots[0];
		if (firstSlot == null)
		{
			return;
		}

		// Ensure text components are resolved before setting display text
		if (firstSlot.playerNameTmpText == null || firstSlot.classNameTmpText == null)
		{
			ResolveSlotAvatarReferences();
		}

		string classLabel = GetClassLabel(GetAvatarKind(className));
		string playerName = PlayerPrefs.GetString("PLAYER_DISPLAY_NAME", "Player 1");
		
		SetSlotDisplayText(firstSlot, playerName, classLabel);

		if (enableLegacyAvatarSync)
		{
			SetSlotClass(0, className);
		}

		// Schedule a refresh in the next frame to sync with session.
		if (gameObject != null && gameObject.activeSelf)
		{
			StartCoroutine(RefreshLobbyOnNextFrame());
		}
	}

	private System.Collections.IEnumerator RefreshLobbyOnNextFrame()
	{
		yield return null;
		RefreshLobbyUI();
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
		SharedRoomSessionManager sessionManager = SharedRoomSessionManager.Instance;
		bool hasActiveSession = sessionManager != null && sessionManager.HasActiveSession;

		if (playerAvatarSlots != null)
		{
			if (enableLegacyAvatarSync && !hasActiveSession)
			{
				ApplyDefaultLocalSlotIfNeeded();
			}

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

				if (occupied && enableLegacyAvatarSync)
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
				if (slot.playerNameTmpText != null)
				{
					slot.slotRoot = slot.playerNameTmpText.transform.parent != null ? slot.playerNameTmpText.transform.parent.gameObject : slot.playerNameTmpText.gameObject;
				}
				else if (slot.classNameTmpText != null)
				{
					slot.slotRoot = slot.classNameTmpText.transform.parent != null ? slot.classNameTmpText.transform.parent.gameObject : slot.classNameTmpText.gameObject;
				}
			}

			if (slot.slotRoot == null)
			{
				continue;
			}

			if (slot.playerNameTmpText == null)
			{
				slot.playerNameTmpText = FindTextByKeyword(slot.slotRoot.transform, "Player Name");
			}

			if (slot.classNameTmpText == null)
			{
				slot.classNameTmpText = FindTextByKeyword(slot.slotRoot.transform, "Class Name");
			}
		}
	}

	private void AutoDiscoverPlayerSlots()
	{
		// Try to find all slot root objects by searching the whole UI hierarchy.
		Transform canvasTransform = gameObject.transform;
		List<GameObject> slotRoots = new List<GameObject>();
		CollectSlotRoots(canvasTransform, slotRoots);

		// Sort by name for consistent ordering
		slotRoots.Sort((a, b) => a.name.CompareTo(b.name));

		if (slotRoots.Count == 0)
		{
			playerAvatarSlots = new PlayerAvatarSlot[0];
			return;
		}

		// Create PlayerAvatarSlot instances for each discovered slot
		playerAvatarSlots = new PlayerAvatarSlot[slotRoots.Count];
		for (int i = 0; i < slotRoots.Count; i++)
		{
			playerAvatarSlots[i] = new PlayerAvatarSlot();
			playerAvatarSlots[i].slotRoot = slotRoots[i];
		}
	}

	private static void CollectSlotRoots(Transform root, List<GameObject> slotRoots)
	{
		if (root == null || slotRoots == null)
		{
			return;
		}

		for (int i = 0; i < root.childCount; i++)
		{
			Transform child = root.GetChild(i);
			if (child == null)
			{
				continue;
			}

			if (IsLikelyPlayerSlotName(child.name))
			{
				slotRoots.Add(child.gameObject);
			}

			CollectSlotRoots(child, slotRoots);
		}
	}

	private static bool IsLikelyPlayerSlotName(string objectName)
	{
		if (string.IsNullOrWhiteSpace(objectName))
		{
			return false;
		}

		string loweredName = objectName.Trim().ToLowerInvariant();
		return loweredName.StartsWith("slot");
	}

	private static TMP_Text FindTextByKeyword(Transform root, string keyword)
	{
		if (root == null || string.IsNullOrEmpty(keyword))
		{
			return null;
		}

		string loweredKeyword = keyword.ToLowerInvariant();
		TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
		for (int i = 0; i < texts.Length; i++)
		{
			TMP_Text candidate = texts[i];
			if (candidate == null)
			{
				continue;
			}

			if (candidate.name.ToLowerInvariant().Contains(loweredKeyword))
			{
				return candidate;
			}
		}

		return null;
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
				SetSlotDisplayText(slot, emptyPlayerName, emptyClassName);
				continue;
			}

			PlayerRef player = players[i];
			SharedPlayerClassType classType = sessionManager.GetPlayerClass(player, SharedPlayerClassType.Unknown);
			AvatarKind resolvedAvatar = ToAvatarKind(classType);
			string className = GetClassLabel(resolvedAvatar);

			string playerName = sessionManager.GetPlayerName(player, $"Player {i + 1}");

			SetSlotDisplayText(slot, playerName, className);
			if (resolvedAvatar != AvatarKind.None)
			{
				slot.avatarKind = resolvedAvatar;
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

		// Ensure text components are resolved
		if (firstSlot.playerNameTmpText == null || firstSlot.classNameTmpText == null)
		{
			ResolveSlotAvatarReferences();
		}

		firstSlot.avatarKind = GetAvatarKind(ClassChoose.LastConfirmedClassName);
		if (firstSlot.avatarKind == AvatarKind.None)
		{
			firstSlot.avatarKind = GetAvatarKind(defaultClassName);
		}

		// Load local player name from PlayerPrefs.
		string localPlayerName = PlayerPrefs.GetString("PLAYER_DISPLAY_NAME", "Player 1");

		SetSlotDisplayText(firstSlot, localPlayerName, GetClassLabel(firstSlot.avatarKind));
	}

	private void ApplyAvatarKindToSlot(PlayerAvatarSlot slot, AvatarKind avatarKind)
	{
		if (slot == null)
		{
			return;
		}

		SetSlotDisplayText(slot, ReadCurrentPlayerLabel(slot), GetClassLabel(avatarKind));
	}

	private void SetSlotVisible(PlayerAvatarSlot slot, bool visible)
	{
		if (slot == null)
		{
			return;
		}

		if (slot.slotRoot == null)
		{
			if (slot.playerNameTmpText != null)
			{
				slot.slotRoot = slot.playerNameTmpText.transform.parent != null ? slot.playerNameTmpText.transform.parent.gameObject : slot.playerNameTmpText.gameObject;
			}
			else if (slot.classNameTmpText != null)
			{
				slot.slotRoot = slot.classNameTmpText.transform.parent != null ? slot.classNameTmpText.transform.parent.gameObject : slot.classNameTmpText.gameObject;
			}
		}

		if (slot.slotRoot != null)
		{
			slot.slotRoot.SetActive(visible);
		}

		if (visible)
		{
			EnsureSlotTextObjectsActive(slot);
		}

		if (!visible)
		{
			SetSlotDisplayText(slot, emptyPlayerName, emptyClassName);
		}
	}

	private static void EnsureSlotTextObjectsActive(PlayerAvatarSlot slot)
	{
		if (slot == null)
		{
			return;
		}

		if (slot.playerNameTmpText != null)
		{
			slot.playerNameTmpText.gameObject.SetActive(true);
		}

		if (slot.classNameTmpText != null)
		{
			slot.classNameTmpText.gameObject.SetActive(true);
		}
	}

	private void SetSlotDisplayText(PlayerAvatarSlot slot, string playerName, string className)
	{
		if (slot == null)
		{
			return;
		}

		string safePlayerName = string.IsNullOrWhiteSpace(playerName) ? emptyPlayerName : playerName.Trim();
		string safeClassName = string.IsNullOrWhiteSpace(className) ? emptyClassName : className.Trim();

		if (slot.playerNameTmpText != null)
		{
			slot.playerNameTmpText.text = safePlayerName;
		}

		if (slot.classNameTmpText != null)
		{
			slot.classNameTmpText.text = safeClassName;
		}
	}

	private string ReadCurrentPlayerLabel(PlayerAvatarSlot slot)
	{
		if (slot == null)
		{
			return emptyPlayerName;
		}

		if (slot.playerNameTmpText != null)
		{
			return slot.playerNameTmpText.text;
		}

		return emptyPlayerName;
	}

	private static string GetClassLabel(AvatarKind avatarKind)
	{
		switch (avatarKind)
		{
			case AvatarKind.Knight:
				return "Knight";
			case AvatarKind.Archer:
				return "Archer";
			default:
				return "CLASS NAME";
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
