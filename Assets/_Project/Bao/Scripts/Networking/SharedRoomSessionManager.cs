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
    private const string PlayerClassPrefsKey = "PLAYER_CLASS_NAME";
    public const string OwnerPropertyKey = "owner";
    public const string ForceCloseRoomPropertyKey = "force_close_room";
    private const string PlayerStatePropertyKeyPrefix = "state_";

    public static SharedRoomSessionManager Instance { get; private set; }

    public static event System.Action OnSessionPropertiesChanged;

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
    private string pendingLocalPlayerName;
    private bool hasPendingLocalPlayerName;
    private int lastPublishedLocalSlotIndex = -1;
    private int lastPublishedLocalState = int.MinValue;
    private string lastPublishedLocalProfilePayload = string.Empty;
    private string lastPublishedLocalStateKey = string.Empty;
    private bool isForceCloseHandling;
    private bool recreateRunnerOnNextResolve;

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

        HandleForceCloseSignal();
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

    public async Task LeaveRoom()
    {
        await ShutdownRunnerAsync();
    }

    public async Task RemoveRoomAndKickAll()
    {
        if (!HasActiveSession)
        {
            await ShutdownRunnerAsync();
            return;
        }

        if (!IsLocalPlayerOwner())
        {
            Debug.LogWarning("SharedRoomSessionManager: chi owner moi duoc Remove Room.");
            return;
        }

        var updates = new Dictionary<string, SessionProperty>
        {
            [ForceCloseRoomPropertyKey] = 1,
        };

        TryUpdateSessionProperties(updates);
        await Task.Yield();
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
        ApplyPendingLocalState();
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
        ApplyPendingLocalState();
        return true;
    }

    public bool SetLocalPlayerName(string playerName)
    {
        string normalized = NormalizePlayerName(playerName);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        pendingLocalPlayerName = normalized;
        hasPendingLocalPlayerName = true;

        PlayerPrefs.SetString("PLAYER_DISPLAY_NAME", normalized);
        PlayerPrefs.Save();
        ApplyPendingLocalState();
        return true;
    }

    public void FlushLocalStateToSession()
    {
        ApplyPendingLocalState();
    }

    public string GetPlayerName(PlayerRef player, string fallback = "Player")
    {
        if (runner != null && runner.LocalPlayer == player && hasPendingLocalPlayerName && !string.IsNullOrWhiteSpace(pendingLocalPlayerName))
        {
            if (verboseLogs)
            {
                Debug.Log($"SharedRoomSessionManager: GetPlayerName({player.RawEncoded}) PENDING -> {pendingLocalPlayerName}");
            }
            return pendingLocalPlayerName;
        }

        string stateKey = ResolvePlayerStatePropertyKey(player);
        if (TryGetPlayerStateProperty(player, out SessionProperty property) && TryDecodePlayerProfile(property, out _, out string synchronizedName) && !string.IsNullOrWhiteSpace(synchronizedName))
        {
            if (verboseLogs)
            {
                Debug.Log($"SharedRoomSessionManager: GetPlayerName({player.RawEncoded}) key={stateKey} SESSION -> {synchronizedName}");
            }
            return synchronizedName;
        }

        if (verboseLogs)
        {
            Debug.LogWarning($"SharedRoomSessionManager: GetPlayerName({player.RawEncoded}) key={stateKey} property not found or decode failed");
        }

        if (runner != null && runner.LocalPlayer == player)
        {
            string localName = PlayerPrefs.GetString("PLAYER_DISPLAY_NAME", string.Empty);
            if (!string.IsNullOrWhiteSpace(localName))
            {
                if (verboseLogs)
                {
                    Debug.Log($"SharedRoomSessionManager: GetPlayerName({player.RawEncoded}) PLAYERPREFS -> {localName}");
                }
                return localName;
            }
        }

        if (verboseLogs)
        {
            Debug.Log($"SharedRoomSessionManager: GetPlayerName({player.RawEncoded}) FALLBACK -> {fallback}");
        }
        return fallback;
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

        if (!TryGetPlayerStateProperty(player, out SessionProperty property))
        {
            if (runner != null && runner.LocalPlayer == player && ClassChoose.LastConfirmedClassName != null)
            {
                SharedPlayerClassType localFallback = SharedPlayerClassTypeUtility.FromClassName(ClassChoose.LastConfirmedClassName);
                if (localFallback != SharedPlayerClassType.Unknown)
                {
                    return localFallback;
                }
            }

            if (runner != null && runner.LocalPlayer == player)
            {
                SharedPlayerClassType prefsFallback = SharedPlayerClassTypeUtility.FromClassName(PlayerPrefs.GetString(PlayerClassPrefsKey, string.Empty));
                if (prefsFallback != SharedPlayerClassType.Unknown)
                {
                    return prefsFallback;
                }
            }

            return fallback;
        }

        if (!TryDecodePlayerState(property, out int encodedState))
        {
            return fallback;
        }

        return SharedPlayerClassTypeUtility.DecodePlayerClass(encodedState, fallback);
    }

    public bool IsPlayerReady(PlayerRef player)
    {
        if (runner != null && runner.LocalPlayer == player && hasPendingLocalReady)
        {
            return pendingLocalReady;
        }

        if (!TryGetPlayerStateProperty(player, out SessionProperty property))
        {
            return runner != null && runner.LocalPlayer == player && hasPendingLocalReady ? pendingLocalReady : false;
        }

        if (!TryDecodePlayerState(property, out int encodedState))
        {
            return false;
        }

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

    public bool TryGetPlayerProfileBySlot(int slotOneBased, out SharedPlayerClassType classType, out string playerName)
    {
        classType = SharedPlayerClassType.Unknown;
        playerName = string.Empty;

        if (slotOneBased <= 0)
        {
            return false;
        }

        string key = $"state_{slotOneBased}";
        if (!TryGetSessionProperty(key, out SessionProperty property))
        {
            return false;
        }

        if (!TryDecodePlayerProfile(property, out int encodedState, out string synchronizedName))
        {
            return false;
        }

        classType = SharedPlayerClassTypeUtility.DecodePlayerClass(encodedState, SharedPlayerClassType.Unknown);
        playerName = synchronizedName;
        return classType != SharedPlayerClassType.Unknown || !string.IsNullOrWhiteSpace(playerName);
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

        orderedPlayersCache.Sort((left, right) => left.RawEncoded.CompareTo(right.RawEncoded));
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

        SessionProperty property;
        string primaryKey = ResolvePlayerStatePropertyKey(sourceRunner, player);
        if (!properties.TryGetValue(primaryKey, out property))
        {
            string rawKey = GetRawPlayerStatePropertyKey(player);
            if (string.Equals(rawKey, primaryKey, StringComparison.OrdinalIgnoreCase) || !properties.TryGetValue(rawKey, out property))
            {
                return false;
            }
        }

        if (!TryDecodePlayerState(property, out int encodedState))
        {
            return false;
        }

        classType = SharedPlayerClassTypeUtility.DecodePlayerClass(encodedState, SharedPlayerClassType.Unknown);
        return classType != SharedPlayerClassType.Unknown;
    }

    private static string ResolvePlayerStatePropertyKey(NetworkRunner sourceRunner, PlayerRef player)
    {
        if (sourceRunner == null || !sourceRunner.IsRunning || !player.IsRealPlayer)
        {
            return "state_1";
        }

        List<PlayerRef> orderedPlayers = new List<PlayerRef>();
        foreach (PlayerRef activePlayer in sourceRunner.ActivePlayers)
        {
            if (activePlayer.IsRealPlayer)
            {
                orderedPlayers.Add(activePlayer);
            }
        }

        orderedPlayers.Sort((left, right) => left.RawEncoded.CompareTo(right.RawEncoded));

        for (int i = 0; i < orderedPlayers.Count; i++)
        {
            if (orderedPlayers[i] == player)
            {
                return $"state_{i + 1}";
            }
        }

        return GetRawPlayerStatePropertyKey(player);
    }

    private static string GetRawPlayerStatePropertyKey(PlayerRef player)
    {
        if (!player.IsRealPlayer)
        {
            return "state_1";
        }

        return $"state_{player.RawEncoded}";
    }

    private bool TryGetPlayerSlotIndex(PlayerRef player, out int slotIndex)
    {
        slotIndex = -1;
        if (!HasActiveSession || runner == null || !player.IsRealPlayer)
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
                runner = CreateFreshRunner();
                RegisterCallbacksIfNeeded();
            }

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
            lastPublishedLocalProfilePayload = string.Empty;
            lastPublishedLocalStateKey = string.Empty;

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

        if (runner != null)
        {
            Destroy(runner);
            runner = null;
            callbacksRegistered = false;
        }

        recreateRunnerOnNextResolve = true;
        connectedPlayers.Clear();
        currentRoomId = null;
        lastPublishedLocalSlotIndex = -1;
        lastPublishedLocalState = int.MinValue;
        lastPublishedLocalProfilePayload = string.Empty;
        lastPublishedLocalStateKey = string.Empty;
    }

    private void ResolveRunner()
    {
        if (runner != null && !runner.IsRunning)
        {
            runner = null;
        }

        if (runner == null && recreateRunnerOnNextResolve)
        {
            runner = CreateFreshRunner();
            recreateRunnerOnNextResolve = false;
        }

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
            runner = CreateFreshRunner();
        }

        if (runner != null)
        {
            runner.ProvideInput = true;
            if (dontDestroyOnLoad && runner.gameObject != gameObject)
            {
                DontDestroyOnLoad(runner.gameObject);
            }
        }
    }

    private NetworkRunner CreateFreshRunner()
    {
        GameObject runnerObject = new GameObject($"{nameof(SharedRoomSessionManager)}_{nameof(NetworkRunner)}");
        NetworkRunner freshRunner = runnerObject.AddComponent<NetworkRunner>();

        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(runnerObject);
        }

        return freshRunner;
    }

    private void ApplyPendingLocalState()
    {
        if (!HasActiveSession || !runner.LocalPlayer.IsRealPlayer)
        {
            return;
        }

        EnsureStateSlotProperties();

        SharedPlayerClassType classToStore = hasPendingLocalClass && pendingLocalClass != SharedPlayerClassType.Unknown
            ? pendingLocalClass
            : SharedPlayerClassTypeUtility.FromClassName(ClassChoose.LastConfirmedClassName);

        if (classToStore == SharedPlayerClassType.Unknown)
        {
            classToStore = SharedPlayerClassTypeUtility.FromClassName(PlayerPrefs.GetString(PlayerClassPrefsKey, string.Empty));
        }

        if (classToStore == SharedPlayerClassType.Unknown)
        {
            classToStore = GetPlayerClass(runner.LocalPlayer, SharedPlayerClassType.Unknown);
        }

        bool readyToStore = hasPendingLocalReady
            ? pendingLocalReady
            : IsPlayerReady(runner.LocalPlayer);

        string nameToStore = hasPendingLocalPlayerName
            ? pendingLocalPlayerName
            : PlayerPrefs.GetString("PLAYER_DISPLAY_NAME", string.Empty);
        nameToStore = NormalizePlayerName(nameToStore);

        int encodedState = SharedPlayerClassTypeUtility.EncodePlayerState(classToStore, readyToStore);
        string profilePayload = BuildPlayerProfilePayload(encodedState, nameToStore);
        string playerIdKey = ResolvePlayerStatePropertyKey(runner.LocalPlayer);

        bool shouldRepublish = hasPendingLocalClass
            || hasPendingLocalReady
            || hasPendingLocalPlayerName
            || encodedState != lastPublishedLocalState
            || !string.Equals(profilePayload, lastPublishedLocalProfilePayload, StringComparison.Ordinal)
            || !string.Equals(playerIdKey, lastPublishedLocalStateKey, StringComparison.OrdinalIgnoreCase);

        if (!shouldRepublish)
        {
            return;
        }

        if (verboseLogs)
        {
            Debug.Log($"SharedRoomSessionManager: ApplyPendingLocalState publish player={runner.LocalPlayer.RawEncoded} key={playerIdKey} payload='{profilePayload}' (class={classToStore}, ready={readyToStore}, name='{nameToStore}')");
        }

        Dictionary<string, SessionProperty> updates = null;
        updates ??= new Dictionary<string, SessionProperty>();
        updates[playerIdKey] = profilePayload;

        if (updates == null || updates.Count == 0)
        {
            return;
        }

        if (TryUpdateSessionProperties(updates))
        {
            hasPendingLocalClass = false;
            hasPendingLocalReady = false;
            hasPendingLocalPlayerName = false;
            lastPublishedLocalState = encodedState;
            lastPublishedLocalProfilePayload = profilePayload;
            lastPublishedLocalStateKey = playerIdKey;
            if (TryGetPlayerSlotIndex(runner.LocalPlayer, out int localSlotIndex))
            {
                lastPublishedLocalSlotIndex = localSlotIndex;
            }
            
            if (verboseLogs)
            {
                Debug.Log($"SharedRoomSessionManager: published {playerIdKey} = name:{nameToStore}, class:{classToStore}");
            }
        }
        else if (verboseLogs)
        {
            Debug.LogWarning($"SharedRoomSessionManager: failed to publish state for player {runner.LocalPlayer.RawEncoded} (will retry next frame)");
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

        const int maxCustomSessionProperties = 10;
        int propertiesCount = sessionInfo.Properties != null ? sessionInfo.Properties.Count : 0;
        
        if (verboseLogs)
        {
            Debug.Log($"SharedRoomSessionManager.TryUpdateSessionProperties: current count={propertiesCount}, keys={string.Join(", ", updates.Keys)}, maxAllowed={maxCustomSessionProperties}");
        }
        int newKeyCount = 0;

        Dictionary<string, SessionProperty> filteredUpdates = null;
        foreach (KeyValuePair<string, SessionProperty> item in updates)
        {
            if (string.IsNullOrEmpty(item.Key))
            {
                continue;
            }

            bool keyExists = sessionInfo.Properties != null && sessionInfo.Properties.ContainsKey(item.Key);
            if (!keyExists)
            {
                if (!IsPlayerStatePropertyKey(item.Key))
                {
                    continue;
                }

                if (propertiesCount + newKeyCount >= maxCustomSessionProperties)
                {
                    continue;
                }

                newKeyCount++;
            }

            filteredUpdates ??= new Dictionary<string, SessionProperty>();
            filteredUpdates[item.Key] = item.Value;
        }

        if (filteredUpdates == null || filteredUpdates.Count == 0)
        {
            return false;
        }

        sessionInfo.UpdateCustomProperties(filteredUpdates);
        
        // Notify listeners that session properties have changed
        OnSessionPropertiesChanged?.Invoke();
        
        return true;
    }

    private void EnsureStateSlotProperties()
    {
        if (!HasActiveSession || runner == null || !runner.LocalPlayer.IsRealPlayer || !IsLocalPlayerOwner())
        {
            return;
        }

        SessionInfo sessionInfo = runner.SessionInfo;
        if (!sessionInfo)
        {
            return;
        }

        IReadOnlyDictionary<string, SessionProperty> properties = sessionInfo.Properties;
        if (properties == null)
        {
            return;
        }

        Dictionary<string, SessionProperty> missingStateKeys = null;
        int defaultState = SharedPlayerClassTypeUtility.EncodePlayerState(SharedPlayerClassType.Unknown, false);
        string defaultPayload = BuildPlayerProfilePayload(defaultState, string.Empty);

        for (int slot = 1; slot <= MaxPlayersPerRoom; slot++)
        {
            string key = $"state_{slot}";
            if (properties.ContainsKey(key))
            {
                continue;
            }

            missingStateKeys ??= new Dictionary<string, SessionProperty>();
            missingStateKeys[key] = defaultPayload;
        }

        if (missingStateKeys != null && missingStateKeys.Count > 0)
        {
            TryUpdateSessionProperties(missingStateKeys);
        }
    }

    private static bool IsPlayerStatePropertyKey(string key)
    {
        return !string.IsNullOrEmpty(key) && key.StartsWith(PlayerStatePropertyKeyPrefix, StringComparison.OrdinalIgnoreCase);
    }

    private Dictionary<string, SessionProperty> BuildInitialSessionProperties()
    {
        var properties = new Dictionary<string, SessionProperty>(2 + MaxPlayersPerRoom)
        {
            [OwnerPropertyKey] = 0,
            [ForceCloseRoomPropertyKey] = 0,
        };

        int defaultState = SharedPlayerClassTypeUtility.EncodePlayerState(SharedPlayerClassType.Unknown, false);
        string defaultPayload = BuildPlayerProfilePayload(defaultState, string.Empty);
        for (int slot = 1; slot <= MaxPlayersPerRoom; slot++)
        {
            properties[$"state_{slot}"] = defaultPayload;
        }

        return properties;
    }

    private bool TryGetPlayerStateProperty(PlayerRef player, out SessionProperty property)
    {
        string primaryKey = ResolvePlayerStatePropertyKey(player);
        if (TryGetSessionProperty(primaryKey, out property))
        {
            if (verboseLogs)
            {
                Debug.Log($"SharedRoomSessionManager: TryGetPlayerStateProperty({player.RawEncoded}) PRIMARY {primaryKey} = success");
            }
            return true;
        }

        string rawKey = GetRawPlayerStatePropertyKey(player);
        if (!string.Equals(rawKey, primaryKey, StringComparison.OrdinalIgnoreCase) && TryGetSessionProperty(rawKey, out property))
        {
            if (verboseLogs)
            {
                Debug.Log($"SharedRoomSessionManager: TryGetPlayerStateProperty({player.RawEncoded}) RAW {rawKey} = success");
            }
            return true;
        }

        if (verboseLogs)
        {
            Debug.LogWarning($"SharedRoomSessionManager: TryGetPlayerStateProperty({player.RawEncoded}) = NOT FOUND (primary:{primaryKey}, raw:{rawKey})");
        }

        property = default;
        return false;
    }

    private string ResolvePlayerStatePropertyKey(PlayerRef player)
    {
        if (TryGetPlayerSlotIndex(player, out int slotIndex))
        {
            string key = $"state_{slotIndex + 1}";
            if (verboseLogs)
            {
                Debug.Log($"SharedRoomSessionManager: ResolvePlayerStatePropertyKey({player.RawEncoded}) SLOT -> {key}");
            }
            return key;
        }

        string rawKey = GetRawPlayerStatePropertyKey(player);
        if (verboseLogs)
        {
            Debug.LogWarning($"SharedRoomSessionManager: ResolvePlayerStatePropertyKey({player.RawEncoded}) RAW_FALLBACK -> {rawKey}");
        }

        return rawKey;
    }



    private static string BuildPlayerProfilePayload(int encodedState, string playerName)
    {
        string safeName = string.IsNullOrWhiteSpace(playerName) ? string.Empty : playerName.Trim().Replace("|", "/");
        return $"{encodedState}|{safeName}";
    }

    private static bool TryDecodePlayerProfile(SessionProperty property, out int encodedState, out string playerName)
    {
        encodedState = SharedPlayerClassTypeUtility.EncodePlayerState(SharedPlayerClassType.Unknown, false);
        playerName = string.Empty;

        if (property.IsString)
        {
            string payload = property;
            if (string.IsNullOrWhiteSpace(payload))
            {
                return false;
            }

            int separatorIndex = payload.IndexOf('|');
            if (separatorIndex <= 0)
            {
                return false;
            }

            string encodedPart = payload.Substring(0, separatorIndex);
            if (!int.TryParse(encodedPart, out encodedState))
            {
                return false;
            }

            playerName = NormalizePlayerName(payload.Substring(separatorIndex + 1));
            return true;
        }

        if (property.IsInt)
        {
            encodedState = property;
            return true;
        }

        return false;
    }

    private static bool TryDecodePlayerState(SessionProperty property, out int encodedState)
    {
        encodedState = SharedPlayerClassTypeUtility.EncodePlayerState(SharedPlayerClassType.Unknown, false);
        return TryDecodePlayerProfile(property, out encodedState, out _);
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

    private void HandleForceCloseSignal()
    {
        if (isForceCloseHandling || !HasActiveSession)
        {
            return;
        }

        if (!TryGetSessionProperty(ForceCloseRoomPropertyKey, out SessionProperty property))
        {
            return;
        }

        int forceClose = property;
        if (forceClose <= 0)
        {
            return;
        }

        isForceCloseHandling = true;
        _ = LeaveRoom();
    }

    private static string NormalizeRoomId(string roomId)
    {
        if (string.IsNullOrWhiteSpace(roomId))
        {
            return string.Empty;
        }

        return roomId.Trim();
    }

    private static string NormalizePlayerName(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
        {
            return string.Empty;
        }

        string trimmed = playerName.Trim();
        const int maxLength = 20;
        return trimmed.Length > maxLength ? trimmed.Substring(0, maxLength) : trimmed;
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
        ApplyPendingLocalState();
        
        // Notify that session properties might have changed (new player could have class data)
        OnSessionPropertiesChanged?.Invoke();
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

        isForceCloseHandling = false;
        connectedPlayers.Clear();
        currentRoomId = null;
        
        // Clear pending local state so it doesn't carry over to the next room
        hasPendingLocalClass = false;
        hasPendingLocalReady = false;
        hasPendingLocalPlayerName = false;
        pendingLocalClass = SharedPlayerClassType.Unknown;
        pendingLocalReady = false;
        pendingLocalPlayerName = null;
        lastPublishedLocalState = int.MinValue;
        lastPublishedLocalProfilePayload = string.Empty;
        lastPublishedLocalStateKey = string.Empty;
        
        // Notify listeners that session has shutdown (so UI can reset)
        OnSessionPropertiesChanged?.Invoke();
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
