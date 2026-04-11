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

    private readonly HashSet<PlayerRef> connectedPlayers = new HashSet<PlayerRef>();
    private readonly List<PlayerRef> orderedPlayersCache = new List<PlayerRef>();

    private bool callbacksRegistered;
    private bool isBusy;
    private string currentRoomId;
    private SharedPlayerClassType pendingLocalClass = SharedPlayerClassType.Unknown;
    private bool hasPendingLocalClass;
    private bool pendingLocalReady;
    private bool hasPendingLocalReady;

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

        if (!TryGetSessionProperty(SharedPlayerClassTypeUtility.GetClassPropertyKey(player), out SessionProperty property))
        {
            if (player.IsRealPlayer && ClassChoose.LastConfirmedClassName != null)
            {
                SharedPlayerClassType localFallback = SharedPlayerClassTypeUtility.FromClassName(ClassChoose.LastConfirmedClassName);
                if (localFallback != SharedPlayerClassType.Unknown)
                {
                    return localFallback;
                }
            }

            return fallback;
        }

        return SharedPlayerClassTypeUtility.ResolveFromSessionProperty(property, fallback);
    }

    public bool IsPlayerReady(PlayerRef player)
    {
        if (runner != null && runner.LocalPlayer == player && hasPendingLocalReady)
        {
            return pendingLocalReady;
        }

        if (!TryGetSessionProperty(SharedPlayerClassTypeUtility.GetReadyPropertyKey(player), out SessionProperty property))
        {
            return runner != null && runner.LocalPlayer == player && hasPendingLocalReady ? pendingLocalReady : false;
        }

        return property;
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

        if (!properties.TryGetValue(SharedPlayerClassTypeUtility.GetClassPropertyKey(player), out SessionProperty property))
        {
            return false;
        }

        classType = SharedPlayerClassTypeUtility.ResolveFromSessionProperty(property, SharedPlayerClassType.Unknown);
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

        Dictionary<string, SessionProperty> updates = null;

        if (hasPendingLocalClass && pendingLocalClass != SharedPlayerClassType.Unknown)
        {
            updates ??= new Dictionary<string, SessionProperty>();
            updates[SharedPlayerClassTypeUtility.GetClassPropertyKey(runner.LocalPlayer)] = (int)pendingLocalClass;
        }

        if (hasPendingLocalReady)
        {
            updates ??= new Dictionary<string, SessionProperty>();
            updates[SharedPlayerClassTypeUtility.GetReadyPropertyKey(runner.LocalPlayer)] = pendingLocalReady;
        }

        if (updates == null || updates.Count == 0)
        {
            return;
        }

        if (TryUpdateSessionProperties(updates))
        {
            hasPendingLocalClass = false;
            hasPendingLocalReady = false;
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

        sessionInfo.UpdateCustomProperties(updates);
        return true;
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
