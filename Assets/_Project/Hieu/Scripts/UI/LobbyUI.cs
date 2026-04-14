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
	[SerializeField] private bool enableLegacyAvatarSync = true;  // Changed to true for consistent syncing
	[SerializeField] private bool hideLegacyReadyIconsInSlots = false;
	[SerializeField] private string emptyPlayerName = "PLAYER NAME";
	[SerializeField] private string emptyClassName = "CLASS NAME";

	private void Awake()
	{
		Debug.Log($"LobbyUI.Awake() - playerAvatarSlots array length: {(playerAvatarSlots != null ? playerAvatarSlots.Length : 0)}");
		
		// Auto-discover playerAvatarSlots if not manually assigned
		if (playerAvatarSlots == null || playerAvatarSlots.Length == 0)
		{
			Debug.LogWarning("LobbyUI: playerAvatarSlots not assigned in Inspector, attempting auto-discovery...");
			AutoDiscoverPlayerSlots();
		}
		
		// Always resolve slot avatar references first, regardless of enableLegacyAvatarSync
		ResolveSlotAvatarReferences();
		
		if (enableLegacyAvatarSync)
		{
			Debug.Log("LobbyUI: enableLegacyAvatarSync is ON");
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
		}
		RefreshLobbyUI();
	}

	private void Update()
	{
		if (enableLegacyAvatarSync)
		{
			SyncLobbyFromSession();
		}
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
		// Update first slot display immediately when class is chosen.
		Debug.Log($"LobbyUI.SetLocalPlayerClass() called with className='{className}'");

		if (playerAvatarSlots == null || playerAvatarSlots.Length == 0)
		{
			Debug.LogError("LobbyUI: playerAvatarSlots is NULL or empty!");
			return;
		}

		PlayerAvatarSlot firstSlot = playerAvatarSlots[0];
		if (firstSlot == null)
		{
			Debug.LogError("LobbyUI: firstSlot is NULL!");
			return;
		}

		// Ensure text components are resolved before setting display text
		if (firstSlot.playerNameTmpText == null || firstSlot.classNameTmpText == null)
		{
			Debug.Log("LobbyUI: Text components not resolved yet, calling ResolveSlotAvatarReferences()");
			ResolveSlotAvatarReferences();
		}

		string classLabel = GetClassLabel(GetAvatarKind(className));
		string playerName = PlayerPrefs.GetString("PLAYER_DISPLAY_NAME", "Player 1");
		Debug.Log($"LobbyUI: About to call SetSlotDisplayText with playerName='{playerName}', classLabel='{classLabel}'");
		
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
			Debug.LogError("LobbyUI: playerAvatarSlots is NULL!");
			return;
		}

		Debug.Log($"LobbyUI: Resolving {playerAvatarSlots.Length} slots...");

		for (int i = 0; i < playerAvatarSlots.Length; i++)
		{
			PlayerAvatarSlot slot = playerAvatarSlots[i];
			if (slot == null)
			{
				Debug.LogWarning($"LobbyUI: Slot {i} is NULL!");
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
				Debug.LogWarning($"LobbyUI: Slot {i} has no slotRoot!");
				continue;
			}

			if (slot.playerNameTmpText == null)
			{
				slot.playerNameTmpText = FindTextByKeyword(slot.slotRoot.transform, "Player Name");
				if (slot.playerNameTmpText != null)
				{
					Debug.Log($"LobbyUI: Found playerNameTmpText for slot {i}: {slot.playerNameTmpText.name}");
				}
				else
				{
					Debug.LogWarning($"LobbyUI: Could not find playerNameTmpText for slot {i}");
				}
			}

			if (slot.classNameTmpText == null)
			{
				slot.classNameTmpText = FindTextByKeyword(slot.slotRoot.transform, "Class Name");
				if (slot.classNameTmpText != null)
				{
					Debug.Log($"LobbyUI: Found classNameTmpText for slot {i}: {slot.classNameTmpText.name}");
				}
				else
				{
					Debug.LogWarning($"LobbyUI: Could not find classNameTmpText for slot {i}");
				}
			}
		}
	}

	private void AutoDiscoverPlayerSlots()
	{
		// Try to find all slot root objects by looking for children containing "slot" in their names
		Transform canvasTransform = gameObject.transform;
		List<GameObject> slotRoots = new List<GameObject>();

		// Search for GameObjects containing "slot" in their names (case-insensitive)
		foreach (Transform child in canvasTransform)
		{
			if (child.name.ToLowerInvariant().Contains("slot"))
			{
				slotRoots.Add(child.gameObject);
			}
		}

		// Sort by name for consistent ordering
		slotRoots.Sort((a, b) => a.name.CompareTo(b.name));

		if (slotRoots.Count == 0)
		{
			Debug.LogWarning("LobbyUI: Could not auto-discover any player slots!");
			playerAvatarSlots = new PlayerAvatarSlot[0];
			return;
		}

		Debug.Log($"LobbyUI: Auto-discovered {slotRoots.Count} player slots");

		// Create PlayerAvatarSlot instances for each discovered slot
		playerAvatarSlots = new PlayerAvatarSlot[slotRoots.Count];
		for (int i = 0; i < slotRoots.Count; i++)
		{
			playerAvatarSlots[i] = new PlayerAvatarSlot();
			playerAvatarSlots[i].slotRoot = slotRoots[i];
			Debug.Log($"  Slot {i}: {slotRoots[i].name}");
		}
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

			// Local player gets name from PlayerPrefs; remote players get generic names.
			string playerName;
			if (sessionManager.Runner != null && sessionManager.Runner.LocalPlayer == player)
			{
				playerName = PlayerPrefs.GetString("PLAYER_DISPLAY_NAME", $"Player {i + 1}");
			}
			else
			{
				playerName = $"Player {i + 1}";
			}

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
			Debug.Log($"LobbyUI: ApplyDefaultLocalSlotIfNeeded - skipped (autoApply={autoApplyFirstSlotFromClassChoose}, slots={playerAvatarSlots?.Length ?? 0})");
			return;
		}

		PlayerAvatarSlot firstSlot = playerAvatarSlots[0];
		if (firstSlot == null)
		{
			Debug.LogWarning("LobbyUI: ApplyDefaultLocalSlotIfNeeded - firstSlot is NULL");
			return;
		}

		if (firstSlot.avatarKind != AvatarKind.None)
		{
			Debug.Log($"LobbyUI: ApplyDefaultLocalSlotIfNeeded - skipped (avatarKind already set to {firstSlot.avatarKind})");
			return;
		}

		// Ensure text components are resolved
		if (firstSlot.playerNameTmpText == null || firstSlot.classNameTmpText == null)
		{
			Debug.Log("LobbyUI: Text components not resolved yet, calling ResolveSlotAvatarReferences()");
			ResolveSlotAvatarReferences();
		}

		firstSlot.avatarKind = GetAvatarKind(ClassChoose.LastConfirmedClassName);
		if (firstSlot.avatarKind == AvatarKind.None)
		{
			firstSlot.avatarKind = GetAvatarKind(defaultClassName);
		}

		Debug.Log($"LobbyUI: ApplyDefaultLocalSlotIfNeeded - setting avatarKind to {firstSlot.avatarKind}");

		// Load local player name from PlayerPrefs.
		string localPlayerName = PlayerPrefs.GetString("PLAYER_DISPLAY_NAME", "Player 1");
		Debug.Log($"LobbyUI: ApplyDefaultLocalSlotIfNeeded - localPlayerName from PlayerPrefs = '{localPlayerName}'");
		
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

		if (slot.slotRoot != null)
		{
			slot.slotRoot.SetActive(visible);
		}

		if (!visible)
		{
			SetSlotDisplayText(slot, emptyPlayerName, emptyClassName);
		}
	}

	private void SetSlotDisplayText(PlayerAvatarSlot slot, string playerName, string className)
	{
		if (slot == null)
		{
			Debug.LogWarning("LobbyUI: SetSlotDisplayText called with NULL slot!");
			return;
		}

		string safePlayerName = string.IsNullOrWhiteSpace(playerName) ? emptyPlayerName : playerName.Trim();
		string safeClassName = string.IsNullOrWhiteSpace(className) ? emptyClassName : className.Trim();

		Debug.Log($"LobbyUI: SetSlotDisplayText - playerName='{safePlayerName}', className='{safeClassName}'");
		Debug.Log($"  playerNameTmpText: {(slot.playerNameTmpText != null ? slot.playerNameTmpText.name : "NULL")}");
		Debug.Log($"  classNameTmpText: {(slot.classNameTmpText != null ? slot.classNameTmpText.name : "NULL")}");

		if (slot.playerNameTmpText != null)
		{
			slot.playerNameTmpText.text = safePlayerName;
			Debug.Log($"  ✓ Set playerNameTmpText.text = '{safePlayerName}'");
		}
		else
		{
			Debug.LogWarning("  ✗ playerNameTmpText is NULL!");
		}

		if (slot.classNameTmpText != null)
		{
			slot.classNameTmpText.text = safeClassName;
			Debug.Log($"  ✓ Set classNameTmpText.text = '{safeClassName}'");
		}
		else
		{
			Debug.LogWarning("  ✗ classNameTmpText is NULL!");
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
				return "Class Name";
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
