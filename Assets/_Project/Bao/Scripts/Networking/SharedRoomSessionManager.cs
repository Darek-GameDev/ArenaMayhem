using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class SharedRoomSessionManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public const string OwnerPropertyKey = "owner";
    private const string PlayerStateSlotKeyPrefix = "slot_state_";
    private const string LegacyPlayerStateSlotKeyPrefix = "state_";

    public static SharedRoomSessionManager Instance { get; private set; }

    [Header("Room Rules")]
    [SerializeField] private int minPlayersToStart = 1;
    [SerializeField] private int maxPlayersPerRoom = 6;

    [Header("Scene")]
    [SerializeField] private string mainGameSceneName = "MainMap";

    [Header("Runner")]
    [SerializeField] private NetworkRunner runner;
    [SerializeField] private bool createRunnerIfMissing = true;
    [SerializeField] private bool dontDestroyOnLoad = true;

    [Header("Debug")]
    [SerializeField] private bool verboseLogs;
    
    [Header("Session Sync")]
    [SerializeField] private int sessionPropertySlotCount = 8;

    private readonly HashSet<PlayerRef> connectedPlayers = new HashSet<PlayerRef>();
    private readonly List<PlayerRef> orderedPlayersCache = new List<PlayerRef>();

    private bool callbacksRegistered;
    private bool isBusy;
    private string currentRoomId;
    private SharedPlayerClassType pendingLocalClass = SharedPlayerClassType.Unknown;
    private bool hasPendingLocalClass;
    private bool pendingLocalReady;
    private bool hasPendingLocalReady;
    private int lastPublishedLocalSlotIndex = -1;
    private int lastPublishedLocalState = int.MinValue;

    public NetworkRunner Runner => runner;
    public bool IsBusy => isBusy;
    public string CurrentRoomId => currentRoomId;
    public int MinPlayersToStart => Mathf.Max(1, minPlayersToStart);
    public int MaxPlayersPerRoom => Mathf.Max(MinPlayersToStart, maxPlayersPerRoom);
    public bool HasActiveSession => runner != null && runner.IsRunning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }

        ResolveRunner();
        RegisterCallbacksIfNeeded();
    }

    private void OnEnable()
    {
        ResolveRunner();
        RegisterCallbacksIfNeeded();
        ApplyPendingLocalState();
    }

    private void Update()
    {
        if (runner == null)
        {
            ResolveRunner();
            RegisterCallbacksIfNeeded();
        }

        ApplyPendingLocalState();
    }

    private void OnDisable()
    {
        UnregisterCallbacks();
    }

    private void OnDestroy()
    {
        UnregisterCallbacks();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public static SharedRoomSessionManager EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameObject managerObject = new GameObject(nameof(SharedRoomSessionManager));
        return managerObject.AddComponent<SharedRoomSessionManager>();
    }

    public async Task<bool> CreateRoomAsync(string roomId)
    {
        return await StartOrJoinRoomAsync(roomId, allowCreateRoom: true);
    }

    public async Task<bool> JoinRoomAsync(string roomId)
    {
        return await StartOrJoinRoomAsync(roomId, allowCreateRoom: false);
    }

    public async void LeaveRoom()
    {
        await ShutdownRunnerAsync();
    }

    public bool SetLocalClass(SharedPlayerClassType classType)
    {
        if (classType == SharedPlayerClassType.Unknown)
        {
            return false;
        }

        pendingLocalClass = classType;
        hasPendingLocalClass = true;
        return true;
    }

    public bool SetLocalClass(string className)
    {
        return SetLocalClass(SharedPlayerClassTypeUtility.FromClassName(className));
    }

    public bool SetLocalReady(bool isReady)
    {
        pendingLocalReady = isReady;
        hasPendingLocalReady = true;
        return true;
    }

    public bool IsLocalPlayerReady()
    {
        if (hasPendingLocalReady)
        {
            return pendingLocalReady;
        }

        return IsPlayerReady(runner.LocalPlayer);
    }

    public SharedPlayerClassType GetPlayerClass(PlayerRef player, SharedPlayerClassType fallback = SharedPlayerClassType.Unknown)
    {
        if (runner != null && runner.LocalPlayer == player && hasPendingLocalClass && pendingLocalClass != SharedPlayerClassType.Unknown)
        {
            return pendingLocalClass;
        }

        SessionProperty property = default;
        bool hasState = false;
        if (TryGetPlayerSlotIndex(player, out int slotIndex))
        {
            hasState = TryGetSessionProperty(GetPlayerStateSlotPropertyKey(slotIndex), out property);
            if (!hasState)
            {
                hasState = TryGetSessionProperty(GetLegacyPlayerStateSlotPropertyKey(slotIndex), out property);
            }
        }

        if (!hasState)
        {
            hasState = TryGetSessionProperty(SharedPlayerClassTypeUtility.GetPlayerStatePropertyKey(player), out property);
        }

        if (!hasState)
        {
            return fallback;
        }

        int encodedState = property;
        return SharedPlayerClassTypeUtility.DecodePlayerClass(encodedState, fallback);
    }

    public bool IsPlayerReady(PlayerRef player)
    {
        if (runner != null && runner.LocalPlayer == player && hasPendingLocalReady)
        {
            return pendingLocalReady;
        }

        SessionProperty property = default;
        bool hasState = false;
        if (TryGetPlayerSlotIndex(player, out int slotIndex))
        {
            hasState = TryGetSessionProperty(GetPlayerStateSlotPropertyKey(slotIndex), out property);
            if (!hasState)
            {
                hasState = TryGetSessionProperty(GetLegacyPlayerStateSlotPropertyKey(slotIndex), out property);
            }
        }

        if (!hasState)
        {
            hasState = TryGetSessionProperty(SharedPlayerClassTypeUtility.GetPlayerStatePropertyKey(player), out property);
        }

        if (!hasState)
        {
            return runner != null && runner.LocalPlayer == player && hasPendingLocalReady ? pendingLocalReady : false;
        }

        int encodedState = property;
        return SharedPlayerClassTypeUtility.DecodePlayerReady(encodedState);
    }

    public bool IsRoomOwner(PlayerRef player)
    {
        if (!player.IsRealPlayer)
        {
            return false;
        }

        if (TryGetOwnerPlayer(out PlayerRef ownerPlayer))
        {
            return ownerPlayer == player;
        }

        return IsSmallestJoinedPlayer(player);
    }

    public bool IsLocalPlayerOwner()
    {
        return HasActiveSession && IsRoomOwner(runner.LocalPlayer);
    }

    public IReadOnlyList<PlayerRef> GetPlayersOrderedById()
    {
        orderedPlayersCache.Clear();

        if (HasActiveSession && runner != null)
        {
            foreach (PlayerRef player in runner.ActivePlayers)
            {
                if (player.IsRealPlayer)
                {
                    orderedPlayersCache.Add(player);
                }
            }
        }
        else
        {
            foreach (PlayerRef player in connectedPlayers)
            {
                if (player.IsRealPlayer)
                {
                    orderedPlayersCache.Add(player);
                }
            }
        }

        orderedPlayersCache.Sort((a, b) => a.RawEncoded.CompareTo(b.RawEncoded));
        return orderedPlayersCache;
    }

    public int GetJoinedPlayerCount()
    {
        return GetPlayersOrderedById().Count;
    }

    public bool CanStartGame(out string reason)
    {
        reason = string.Empty;

        if (!HasActiveSession)
        {
            reason = "Chua ket noi phong.";
            return false;
        }

        if (!IsLocalPlayerOwner())
        {
            reason = "Chi owner moi duoc Start.";
            return false;
        }

        int playerCount = GetJoinedPlayerCount();
        const int requiredPlayersToStart = 1;
        if (playerCount < requiredPlayersToStart)
        {
            reason = $"Can toi thieu {requiredPlayersToStart} nguoi.";
            return false;
        }

        if (playerCount > MaxPlayersPerRoom)
        {
            reason = $"Vuot qua gioi han {MaxPlayersPerRoom} nguoi.";
            return false;
        }

        if (playerCount == 0)
        {
            reason = "Khong co nguoi choi trong phong.";
            return false;
        }

        return true;
    }

    public bool StartGameIfReady()
    {
        if (!CanStartGame(out string reason))
        {
            if (!string.IsNullOrEmpty(reason))
            {
                Debug.LogWarning($"SharedRoomSessionManager: {reason}");
            }

            return false;
        }

        if (string.IsNullOrWhiteSpace(mainGameSceneName))
        {
            Debug.LogWarning("SharedRoomSessionManager: chua gan ten scene MainGame.");
            return false;
        }

        runner.LoadScene(mainGameSceneName, LoadSceneMode.Single, LocalPhysicsMode.None, true);
        return true;
    }

    public static bool TryGetPlayerClass(NetworkRunner sourceRunner, PlayerRef player, out SharedPlayerClassType classType)
    {
        classType = SharedPlayerClassType.Unknown;

        SharedRoomSessionManager sessionManager = Instance;
        if (sessionManager != null && sessionManager.runner == sourceRunner && sessionManager.hasPendingLocalClass && sourceRunner != null && sourceRunner.LocalPlayer == player && sessionManager.pendingLocalClass != SharedPlayerClassType.Unknown)
        {
            classType = sessionManager.pendingLocalClass;
            return true;
        }

        if (sourceRunner == null || !sourceRunner.IsRunning)
        {
            return false;
        }

        SessionInfo sessionInfo = sourceRunner.SessionInfo;
        if (!sessionInfo)
        {
            return false;
        }

        IReadOnlyDictionary<string, SessionProperty> properties = sessionInfo.Properties;
        if (properties == null)
        {
            return false;
        }

        if (!TryGetPlayerSlotIndex(sourceRunner, player, out int slotIndex))
        {
            return false;
        }

        string slotKey = GetPlayerStateSlotPropertyKey(slotIndex);
        if (!properties.TryGetValue(slotKey, out SessionProperty property))
        {
            string playerKey = SharedPlayerClassTypeUtility.GetPlayerStatePropertyKey(player);
            if (properties.TryGetValue(playerKey, out property))
            {
                int playerEncodedState = property;
                classType = SharedPlayerClassTypeUtility.DecodePlayerClass(playerEncodedState, SharedPlayerClassType.Unknown);
                return classType != SharedPlayerClassType.Unknown;
            }

            string legacySlotKey = GetLegacyPlayerStateSlotPropertyKey(slotIndex);
            if (!properties.TryGetValue(legacySlotKey, out property))
            {
                return false;
            }
        }

        int encodedState = property;
        classType = SharedPlayerClassTypeUtility.DecodePlayerClass(encodedState, SharedPlayerClassType.Unknown);
        return classType != SharedPlayerClassType.Unknown;
    }

    private async Task<bool> StartOrJoinRoomAsync(string roomId, bool allowCreateRoom)
    {
        roomId = NormalizeRoomId(roomId);
        if (string.IsNullOrWhiteSpace(roomId))
        {
            return false;
        }

        if (isBusy)
        {
            return false;
        }

        isBusy = true;

        try
        {
            ResolveRunner();
            RegisterCallbacksIfNeeded();

            if (runner == null)
            {
                Debug.LogWarning("SharedRoomSessionManager: khong tao duoc NetworkRunner.");
                return false;
            }

            if (runner.IsRunning)
            {
                if (runner.SessionInfo && string.Equals(runner.SessionInfo.Name, roomId, StringComparison.OrdinalIgnoreCase))
                {
                    currentRoomId = roomId;
                    return true;
                }

                await ShutdownRunnerAsync();
                ResolveRunner();
                RegisterCallbacksIfNeeded();
            }

            INetworkSceneManager sceneManager = runner.GetComponent<INetworkSceneManager>();
            if (sceneManager == null)
            {
                sceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>();
            }

            INetworkObjectProvider objectProvider = runner.GetComponent<INetworkObjectProvider>();
            if (objectProvider == null)
            {
                objectProvider = runner.gameObject.AddComponent<NetworkObjectProviderDefault>();
            }

            StartGameArgs args = new StartGameArgs
            {
                GameMode = GameMode.Shared,
                SessionName = roomId,
                PlayerCount = MaxPlayersPerRoom,
                SceneManager = sceneManager,
                ObjectProvider = objectProvider,
                IsOpen = true,
                IsVisible = true,
                EnableClientSessionCreation = allowCreateRoom,
                SessionProperties = allowCreateRoom ? BuildInitialSessionProperties() : null,
            };

            StartGameResult result = await runner.StartGame(args);
            if (!result.Ok)
            {
                Debug.LogWarning($"SharedRoomSessionManager: StartGame that bai. {result.ShutdownReason} - {result.ErrorMessage}");
                return false;
            }

            currentRoomId = roomId;
            connectedPlayers.Clear();
            if (runner.LocalPlayer.IsRealPlayer)
            {
                connectedPlayers.Add(runner.LocalPlayer);
            }

            lastPublishedLocalSlotIndex = -1;
            lastPublishedLocalState = int.MinValue;

            ApplyPendingLocalState();
            EnsureOwnerProperty();

            if (verboseLogs)
            {
                Debug.Log($"SharedRoomSessionManager: vao phong '{roomId}' thanh cong.");
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"SharedRoomSessionManager: loi khi StartGame -> {ex.Message}");
            return false;
        }
        finally
        {
            isBusy = false;
        }
    }

    private async Task ShutdownRunnerAsync()
    {
        if (runner == null || !runner.IsRunning)
        {
            connectedPlayers.Clear();
            currentRoomId = null;
            return;
        }

        try
        {
            await runner.Shutdown(false, default, true);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"SharedRoomSessionManager: shutdown loi -> {ex.Message}");
        }

        connectedPlayers.Clear();
        currentRoomId = null;
        lastPublishedLocalSlotIndex = -1;
        lastPublishedLocalState = int.MinValue;
    }

    private void ResolveRunner()
    {
        if (runner == null)
        {
            runner = GetComponent<NetworkRunner>();
        }

        if (runner == null)
        {
            runner = FindFirstObjectByType<NetworkRunner>();
        }

        if (runner == null && createRunnerIfMissing)
        {
            runner = gameObject.AddComponent<NetworkRunner>();
        }

        if (runner != null)
        {
            runner.ProvideInput = true;
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(runner.gameObject);
            }
        }
    }

    private void ApplyPendingLocalState()
    {
        if (!HasActiveSession || !runner.LocalPlayer.IsRealPlayer)
        {
            return;
        }

        SharedPlayerClassType classToStore = hasPendingLocalClass && pendingLocalClass != SharedPlayerClassType.Unknown
            ? pendingLocalClass
            : SharedPlayerClassType.Unknown;

        if (classToStore == SharedPlayerClassType.Unknown)
        {
            classToStore = GetPlayerClass(runner.LocalPlayer, SharedPlayerClassType.Unknown);
        }

        bool readyToStore = hasPendingLocalReady
            ? pendingLocalReady
            : IsPlayerReady(runner.LocalPlayer);

        TryGetPlayerSlotIndex(runner.LocalPlayer, out int localSlotIndex);
        int encodedState = SharedPlayerClassTypeUtility.EncodePlayerState(classToStore, readyToStore);
        bool shouldRepublish = hasPendingLocalClass || hasPendingLocalReady || encodedState != lastPublishedLocalState || localSlotIndex != lastPublishedLocalSlotIndex;
        if (!shouldRepublish)
        {
            return;
        }

        Dictionary<string, SessionProperty> updates = null;
        updates ??= new Dictionary<string, SessionProperty>();

        string playerKey = SharedPlayerClassTypeUtility.GetPlayerStatePropertyKey(runner.LocalPlayer);
        if (TryGetSessionProperty(playerKey, out _))
        {
            updates[playerKey] = encodedState;
        }

        if (localSlotIndex >= 0)
        {
            string slotKey = GetPlayerStateSlotPropertyKey(localSlotIndex);
            if (TryGetSessionProperty(slotKey, out _))
            {
                updates[slotKey] = encodedState;
            }
            else
            {
                string legacySlotKey = GetLegacyPlayerStateSlotPropertyKey(localSlotIndex);
                if (TryGetSessionProperty(legacySlotKey, out _))
                {
                    updates[legacySlotKey] = encodedState;
                }
            }
        }

        if (updates == null || updates.Count == 0)
        {
            return;
        }

        if (TryUpdateSessionProperties(updates))
        {
            hasPendingLocalClass = false;
            hasPendingLocalReady = false;
            lastPublishedLocalState = encodedState;
            lastPublishedLocalSlotIndex = localSlotIndex;
        }
    }

    private void RegisterCallbacksIfNeeded()
    {
        if (runner == null || callbacksRegistered)
        {
            return;
        }

        runner.RemoveCallbacks(this);
        runner.AddCallbacks(this);
        callbacksRegistered = true;
    }

    private void UnregisterCallbacks()
    {
        if (runner == null || !callbacksRegistered)
        {
            return;
        }

        runner.RemoveCallbacks(this);
        callbacksRegistered = false;
    }

    private bool TryUpdateSessionProperties(Dictionary<string, SessionProperty> updates)
    {
        if (!HasActiveSession || updates == null || updates.Count == 0)
        {
            return false;
        }

        SessionInfo sessionInfo = runner.SessionInfo;
        if (!sessionInfo)
        {
            return false;
        }

        Dictionary<string, SessionProperty> filteredUpdates = null;
        foreach (KeyValuePair<string, SessionProperty> item in updates)
        {
            if (string.IsNullOrEmpty(item.Key) || !sessionInfo.Properties.ContainsKey(item.Key))
            {
                continue;
            }

            filteredUpdates ??= new Dictionary<string, SessionProperty>();
            filteredUpdates[item.Key] = item.Value;
        }

        if (filteredUpdates == null || filteredUpdates.Count == 0)
        {
            return false;
        }

        sessionInfo.UpdateCustomProperties(filteredUpdates);
        return true;
    }

    private Dictionary<string, SessionProperty> BuildInitialSessionProperties()
    {
        int slotCount = Mathf.Clamp(Mathf.Max(MaxPlayersPerRoom, sessionPropertySlotCount), 1, 9);
        var properties = new Dictionary<string, SessionProperty>(slotCount + 1)
        {
            [OwnerPropertyKey] = 0,
        };

        for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
        {
            properties[GetPlayerStateSlotPropertyKey(slotIndex)] = SharedPlayerClassTypeUtility.EncodePlayerState(SharedPlayerClassType.Unknown, false);
        }

        return properties;
    }

    private bool TryGetPlayerStateProperty(PlayerRef player, out SessionProperty property)
    {
        return TryGetSessionProperty(SharedPlayerClassTypeUtility.GetPlayerStatePropertyKey(player), out property);
    }

    private bool TryGetPlayerSlotIndex(PlayerRef player, out int slotIndex)
    {
        slotIndex = -1;
        if (!player.IsRealPlayer)
        {
            return false;
        }

        IReadOnlyList<PlayerRef> players = GetPlayersOrderedById();
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] == player)
            {
                slotIndex = i;
                return true;
            }
        }

        return false;
    }

    private static bool TryGetPlayerSlotIndex(NetworkRunner sourceRunner, PlayerRef player, out int slotIndex)
    {
        slotIndex = -1;
        if (sourceRunner == null || !sourceRunner.IsRunning || !player.IsRealPlayer)
        {
            return false;
        }

        List<PlayerRef> sortedPlayers = new List<PlayerRef>();
        foreach (PlayerRef activePlayer in sourceRunner.ActivePlayers)
        {
            if (activePlayer.IsRealPlayer)
            {
                sortedPlayers.Add(activePlayer);
            }
        }

        sortedPlayers.Sort((a, b) => a.RawEncoded.CompareTo(b.RawEncoded));
        for (int i = 0; i < sortedPlayers.Count; i++)
        {
            if (sortedPlayers[i] == player)
            {
                slotIndex = i;
                return true;
            }
        }

        return false;
    }

    private static string GetPlayerStateSlotPropertyKey(int slotIndex)
    {
        return $"{PlayerStateSlotKeyPrefix}{slotIndex}";
    }

    private static string GetLegacyPlayerStateSlotPropertyKey(int slotIndex)
    {
        return $"{LegacyPlayerStateSlotKeyPrefix}{slotIndex}";
    }

    private void EnsureOwnerProperty()
    {
        if (!HasActiveSession || !runner.LocalPlayer.IsRealPlayer)
        {
            return;
        }

        if (TryGetOwnerPlayer(out _))
        {
            return;
        }

        var updates = new Dictionary<string, SessionProperty>
        {
            [OwnerPropertyKey] = runner.LocalPlayer.RawEncoded,
        };

        TryUpdateSessionProperties(updates);
    }

    private bool TryGetOwnerPlayer(out PlayerRef ownerPlayer)
    {
        ownerPlayer = default;

        if (!TryGetSessionProperty(OwnerPropertyKey, out SessionProperty property))
        {
            return false;
        }

        int ownerId = property;
        if (ownerId <= 0)
        {
            return false;
        }

        ownerPlayer = PlayerRef.FromEncoded(ownerId);
        return ownerPlayer.IsRealPlayer;
    }

    private bool TryGetSessionProperty(string key, out SessionProperty property)
    {
        property = default;
        if (string.IsNullOrEmpty(key) || !HasActiveSession)
        {
            return false;
        }

        SessionInfo sessionInfo = runner.SessionInfo;
        if (!sessionInfo)
        {
            return false;
        }

        IReadOnlyDictionary<string, SessionProperty> properties = sessionInfo.Properties;
        if (properties == null)
        {
            return false;
        }

        return properties.TryGetValue(key, out property);
    }

    private bool IsSmallestJoinedPlayer(PlayerRef player)
    {
        if (!player.IsRealPlayer)
        {
            return false;
        }

        PlayerRef smallest = default;
        bool found = false;

        foreach (PlayerRef candidate in GetPlayersOrderedById())
        {
            if (!candidate.IsRealPlayer)
            {
                continue;
            }

            if (!found || candidate.RawEncoded < smallest.RawEncoded)
            {
                smallest = candidate;
                found = true;
            }
        }

        if (!found)
        {
            return HasActiveSession && runner.LocalPlayer == player;
        }

        return smallest == player;
    }

    private static string NormalizeRoomId(string roomId)
    {
        if (string.IsNullOrWhiteSpace(roomId))
        {
            return string.Empty;
        }

        return roomId.Trim();
    }

    void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner sourceRunner, PlayerRef player)
    {
        if (runner == null || sourceRunner != runner)
        {
            return;
        }

        connectedPlayers.Add(player);

        if (verboseLogs)
        {
            Debug.Log($"SharedRoomSessionManager: player joined -> {player.RawEncoded}");
        }

        EnsureOwnerProperty();
    }

    void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner sourceRunner, PlayerRef player)
    {
        if (runner == null || sourceRunner != runner)
        {
            return;
        }

        connectedPlayers.Remove(player);

        if (!TryGetOwnerPlayer(out PlayerRef ownerPlayer))
        {
            return;
        }

        if (ownerPlayer != player)
        {
            return;
        }

        if (!runner.LocalPlayer.IsRealPlayer)
        {
            return;
        }

        if (!IsSmallestJoinedPlayer(runner.LocalPlayer))
        {
            return;
        }

        var updates = new Dictionary<string, SessionProperty>
        {
            [OwnerPropertyKey] = runner.LocalPlayer.RawEncoded,
        };

        TryUpdateSessionProperties(updates);
    }

    void INetworkRunnerCallbacks.OnInput(NetworkRunner sourceRunner, NetworkInput input) { }
    void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner sourceRunner, PlayerRef player, NetworkInput input) { }
    void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner sourceRunner, NetworkObject obj, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner sourceRunner, NetworkObject obj, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnShutdown(NetworkRunner sourceRunner, ShutdownReason shutdownReason)
    {
        if (runner == null || sourceRunner != runner)
        {
            return;
        }

        connectedPlayers.Clear();
        currentRoomId = null;
    }

    void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner sourceRunner) { }
    void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner sourceRunner, NetDisconnectReason reason) { }
    void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner sourceRunner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner sourceRunner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner sourceRunner, SimulationMessagePtr message) { }
    void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner sourceRunner, List<SessionInfo> sessionList) { }
    void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner sourceRunner, Dictionary<string, object> data) { }
    void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner sourceRunner, HostMigrationToken hostMigrationToken) { }
    void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner sourceRunner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    void INetworkRunnerCallbacks.OnReliableDataProgress(NetworkRunner sourceRunner, PlayerRef player, ReliableKey key, float progress) { }
    void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner sourceRunner) { }
    void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner sourceRunner) { }
}
