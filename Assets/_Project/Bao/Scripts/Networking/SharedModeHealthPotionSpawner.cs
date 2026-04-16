using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class SharedModeHealthPotionSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("References")]
    [SerializeField] private NetworkRunner runner;
    [SerializeField] private NetworkObject potionPrefab;
    [SerializeField] private NetworkObject[] randomItemPrefabs;

    [Header("Spawn Rules")]
    [SerializeField] private bool spawnOnFirstPlayerJoin = true;
    [SerializeField] private bool allowSpawnWithF = true;
    [SerializeField] private bool onlyOnePotionAtATime = false;
    [SerializeField, Min(1)] private int initialSpawnCount = 3;
    [SerializeField, Min(1)] private int spawnCountPerFPress = 1;
    [SerializeField, Min(1)] private int maxSpawnedItemsInMap = 10;
    [SerializeField] private string mainMapSceneName = "MainMap";
    [SerializeField, Min(1f)] private float periodicSpawnIntervalSeconds = 15f;
    [SerializeField, Min(1)] private int periodicSpawnCount = 1;
    [SerializeField] private bool restrictSpawnToLowestPlayerId = true;
    [SerializeField] private Transform[] randomSpawnPoints;
    [SerializeField] private Transform fallbackSpawnPoint;
    [SerializeField] private bool logSpawn = true;

    private bool callbacksRegistered;
    private NetworkObject spawnedPotion;
    private readonly List<NetworkObject> visiblePotions = new List<NetworkObject>(4);
    private readonly List<Vector3> reservedSpawnPositions = new List<Vector3>(8);
    private float nextPeriodicSpawnAt = -1f;

    private void Awake()
    {
        TryResolveRunner();
        TryRegisterCallbacks();
    }

    private void OnEnable()
    {
        TryResolveRunner();
        TryRegisterCallbacks();
    }

    private void Update()
    {
        if (!callbacksRegistered)
        {
            TryResolveRunner();
            TryRegisterCallbacks();
        }

        RefreshSpawnedPotionReference();
        HandlePeriodicSpawn();

        if (!allowSpawnWithF || runner == null)
        {
            return;
        }

        if (runner.GameMode != GameMode.Shared || !runner.LocalPlayer.IsRealPlayer)
        {
            return;
        }

        if (!CanSpawnNetworkItems(runner))
        {
            return;
        }

        if (!IsMainMapActive())
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.fKey.wasPressedThisFrame)
        {
            return;
        }

        TrySpawnItems(runner, requireFirstRealPlayer: false, requestedCount: spawnCountPerFPress);
    }

    private void OnDisable()
    {
        UnregisterCallbacks();
    }

    private void OnDestroy()
    {
        UnregisterCallbacks();
    }

    private void TryResolveRunner()
    {
        if (runner == null)
        {
            runner = GetComponent<NetworkRunner>();
        }

        if (runner == null)
        {
            runner = FindFirstObjectByType<NetworkRunner>();
        }
    }

    private void TryRegisterCallbacks()
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

    void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!spawnOnFirstPlayerJoin)
        {
            return;
        }

        // Initial map items must be spawned once by the shared-mode master only.
        if (!runner.IsSharedModeMasterClient || player != runner.LocalPlayer)
        {
            return;
        }

        if (!CanSpawnNetworkItems(runner))
        {
            return;
        }

        if (!IsMainMapActive())
        {
            return;
        }

        int spawnedCount = TrySpawnItems(runner, requireFirstRealPlayer: true, requestedCount: initialSpawnCount);
        if (spawnedCount > 0)
        {
            ScheduleNextPeriodicSpawn();
        }
    }

    void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (GetRealPlayerCount(runner) > 0)
        {
            return;
        }

        CollectExistingPotions(visiblePotions);
        for (int i = 0; i < visiblePotions.Count; i++)
        {
            NetworkObject candidate = visiblePotions[i];
            if (candidate == null || !candidate.HasStateAuthority)
            {
                continue;
            }

            runner.Despawn(candidate);
        }

        spawnedPotion = null;
        nextPeriodicSpawnAt = -1f;
    }

    private int TrySpawnItems(NetworkRunner currentRunner, bool requireFirstRealPlayer, int requestedCount)
    {
        if (currentRunner == null)
        {
            return 0;
        }

        if (currentRunner.GameMode != GameMode.Shared)
        {
            return 0;
        }

        if (!CanSpawnNetworkItems(currentRunner))
        {
            return 0;
        }

        if (requireFirstRealPlayer && GetRealPlayerCount(currentRunner) != 1)
        {
            return 0;
        }

        RefreshSpawnedPotionReference();
        int currentSpawnedCount = visiblePotions.Count;

        if (onlyOnePotionAtATime && currentSpawnedCount > 0)
        {
            return 0;
        }

        int spawnCount = Mathf.Max(1, requestedCount);
        if (onlyOnePotionAtATime)
        {
            spawnCount = 1;
        }
        else if (maxSpawnedItemsInMap > 0)
        {
            int availableSlots = Mathf.Max(0, maxSpawnedItemsInMap - currentSpawnedCount);
            spawnCount = Mathf.Min(spawnCount, availableSlots);
        }

        if (spawnCount <= 0)
        {
            return 0;
        }

        int spawnedCount = 0;
        reservedSpawnPositions.Clear();

        for (int i = 0; i < spawnCount; i++)
        {
            NetworkObject prefabToSpawn = GetRandomSpawnPrefab();
            if (prefabToSpawn == null)
            {
                return spawnedCount;
            }

            if (!TryGetFreeSpawnPosition(reservedSpawnPositions, out Vector3 spawnPosition, out Quaternion spawnRotation))
            {
                return spawnedCount;
            }

            reservedSpawnPositions.Add(spawnPosition);
            spawnedPotion = currentRunner.Spawn(prefabToSpawn, spawnPosition, spawnRotation, currentRunner.LocalPlayer);
            if (spawnedPotion != null)
            {
                spawnedCount++;
            }

            if (logSpawn && spawnedPotion != null)
            {
                Debug.Log($"SharedModeHealthPotionSpawner: spawned item '{spawnedPotion.name}' at {spawnPosition}.");
            }
        }

        return spawnedCount;
    }

    private bool TryGetFreeSpawnPosition(List<Vector3> reservedPositions, out Vector3 position, out Quaternion rotation)
    {
        if (TryGetFreeRandomSpawnPoint(randomSpawnPoints, reservedPositions, out Transform point))
        {
            position = point.position;
            rotation = point.rotation;
            return true;
        }

        if (fallbackSpawnPoint != null && !IsSpawnPositionOccupied(fallbackSpawnPoint.position, reservedPositions))
        {
            position = fallbackSpawnPoint.position;
            rotation = fallbackSpawnPoint.rotation;
            return true;
        }

        position = Vector3.zero;
        rotation = Quaternion.identity;
        return false;
    }

    private bool TryGetFreeRandomSpawnPoint(Transform[] points, List<Vector3> reservedPositions, out Transform selectedPoint)
    {
        selectedPoint = null;
        if (points == null || points.Length == 0)
        {
            return false;
        }

        int startIndex = UnityEngine.Random.Range(0, points.Length);
        for (int i = 0; i < points.Length; i++)
        {
            int index = (startIndex + i) % points.Length;
            Transform candidate = points[index];
            if (candidate == null)
            {
                continue;
            }

            if (IsSpawnPositionOccupied(candidate.position, reservedPositions))
            {
                continue;
            }

            selectedPoint = candidate;
            return true;
        }

        return false;
    }

    private bool IsSpawnPositionOccupied(Vector3 position, List<Vector3> reservedPositions)
    {
        const float occupancyRadius = 0.25f;
        float occupancyRadiusSqr = occupancyRadius * occupancyRadius;

        for (int i = 0; i < reservedPositions.Count; i++)
        {
            if ((reservedPositions[i] - position).sqrMagnitude <= occupancyRadiusSqr)
            {
                return true;
            }
        }

        for (int i = 0; i < visiblePotions.Count; i++)
        {
            NetworkObject existingPotion = visiblePotions[i];
            if (existingPotion == null)
            {
                continue;
            }

            if ((existingPotion.transform.position - position).sqrMagnitude <= occupancyRadiusSqr)
            {
                return true;
            }
        }

        return false;
    }

    private void HandlePeriodicSpawn()
    {
        if (!CanRunPeriodicSpawn())
        {
            nextPeriodicSpawnAt = -1f;
            return;
        }

        if (nextPeriodicSpawnAt < 0f)
        {
            ScheduleNextPeriodicSpawn();
        }

        if (Time.time < nextPeriodicSpawnAt)
        {
            return;
        }

        TrySpawnItems(runner, requireFirstRealPlayer: false, requestedCount: periodicSpawnCount);
        ScheduleNextPeriodicSpawn();
    }

    private bool CanRunPeriodicSpawn()
    {
        if (runner == null)
        {
            return false;
        }

        if (runner.GameMode != GameMode.Shared || !runner.LocalPlayer.IsRealPlayer)
        {
            return false;
        }

        if (!CanSpawnNetworkItems(runner))
        {
            return false;
        }

        if (!IsMainMapActive())
        {
            return false;
        }

        return GetRealPlayerCount(runner) > 0;
    }

    private void ScheduleNextPeriodicSpawn()
    {
        nextPeriodicSpawnAt = Time.time + Mathf.Max(1f, periodicSpawnIntervalSeconds);
    }

    private bool IsMainMapActive()
    {
        if (string.IsNullOrWhiteSpace(mainMapSceneName))
        {
            return true;
        }

        return string.Equals(SceneManager.GetActiveScene().name, mainMapSceneName, StringComparison.OrdinalIgnoreCase);
    }

    private NetworkObject GetRandomSpawnPrefab()
    {
        if (TryGetRandomPrefab(randomItemPrefabs, out NetworkObject selectedPrefab))
        {
            return selectedPrefab;
        }

        return potionPrefab;
    }

    private static bool TryGetRandomPrefab(NetworkObject[] prefabs, out NetworkObject selectedPrefab)
    {
        selectedPrefab = null;
        if (prefabs == null || prefabs.Length == 0)
        {
            return false;
        }

        int startIndex = UnityEngine.Random.Range(0, prefabs.Length);
        for (int i = 0; i < prefabs.Length; i++)
        {
            int index = (startIndex + i) % prefabs.Length;
            if (prefabs[index] != null)
            {
                selectedPrefab = prefabs[index];
                return true;
            }
        }

        return false;
    }

    private Vector3 GetRandomSpawnPosition(out Quaternion rotation)
    {
        if (TryGetRandomSpawnPoint(randomSpawnPoints, out Transform point))
        {
            rotation = point.rotation;
            return point.position;
        }

        if (fallbackSpawnPoint != null)
        {
            rotation = fallbackSpawnPoint.rotation;
            return fallbackSpawnPoint.position;
        }

        rotation = Quaternion.identity;
        return Vector3.zero;
    }

    private static bool TryGetRandomSpawnPoint(Transform[] points, out Transform selectedPoint)
    {
        selectedPoint = null;
        if (points == null || points.Length == 0)
        {
            return false;
        }

        int startIndex = UnityEngine.Random.Range(0, points.Length);
        for (int i = 0; i < points.Length; i++)
        {
            int index = (startIndex + i) % points.Length;
            if (points[index] != null)
            {
                selectedPoint = points[index];
                return true;
            }
        }

        return false;
    }

    private void RefreshSpawnedPotionReference()
    {
        CollectExistingPotions(visiblePotions);

        if (visiblePotions.Count == 0)
        {
            spawnedPotion = null;
            return;
        }

        NetworkObject keeper = ChooseKeeperPotion(visiblePotions);
        spawnedPotion = keeper;

        if (!onlyOnePotionAtATime || visiblePotions.Count <= 1 || runner == null)
        {
            return;
        }

        for (int i = 0; i < visiblePotions.Count; i++)
        {
            NetworkObject candidate = visiblePotions[i];
            if (candidate == null || candidate == keeper)
            {
                continue;
            }

            if (candidate.HasStateAuthority)
            {
                runner.Despawn(candidate);
            }
        }
    }

    private static void CollectExistingPotions(List<NetworkObject> results)
    {
        results.Clear();

        SharedModeHealthPotionItem[] items = FindObjectsByType<SharedModeHealthPotionItem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null || items[i].Object == null)
            {
                continue;
            }

            results.Add(items[i].Object);
        }
    }

    private static NetworkObject ChooseKeeperPotion(List<NetworkObject> potions)
    {
        NetworkObject selected = potions[0];
        int selectedOwner = GetOwnerSortKey(selected);

        for (int i = 1; i < potions.Count; i++)
        {
            NetworkObject candidate = potions[i];
            if (candidate == null)
            {
                continue;
            }

            int candidateOwner = GetOwnerSortKey(candidate);
            if (candidateOwner < selectedOwner)
            {
                selected = candidate;
                selectedOwner = candidateOwner;
            }
        }

        return selected;
    }

    private static int GetOwnerSortKey(NetworkObject networkObject)
    {
        if (networkObject == null)
        {
            return int.MaxValue;
        }

        PlayerRef inputAuthority = networkObject.InputAuthority;
        return inputAuthority.IsRealPlayer ? inputAuthority.PlayerId : int.MaxValue - 1;
    }

    private static int GetRealPlayerCount(NetworkRunner currentRunner)
    {
        int count = 0;
        foreach (PlayerRef activePlayer in currentRunner.ActivePlayers)
        {
            if (activePlayer.IsRealPlayer)
            {
                count++;
            }
        }

        return count;
    }

    private bool CanSpawnNetworkItems(NetworkRunner currentRunner)
    {
        if (currentRunner == null || !currentRunner.LocalPlayer.IsRealPlayer)
        {
            return false;
        }

        if (currentRunner.GameMode != GameMode.Shared)
        {
            return false;
        }

        if (!currentRunner.IsSharedModeMasterClient)
        {
            return false;
        }

        if (!restrictSpawnToLowestPlayerId)
        {
            return true;
        }

        return TryGetLowestRealPlayer(currentRunner, out PlayerRef lowestPlayer) && lowestPlayer == currentRunner.LocalPlayer;
    }

    private static bool TryGetLowestRealPlayer(NetworkRunner currentRunner, out PlayerRef lowestPlayer)
    {
        lowestPlayer = default;
        bool found = false;
        int lowestId = int.MaxValue;

        foreach (PlayerRef activePlayer in currentRunner.ActivePlayers)
        {
            if (!activePlayer.IsRealPlayer)
            {
                continue;
            }

            if (!found || activePlayer.PlayerId < lowestId)
            {
                found = true;
                lowestId = activePlayer.PlayerId;
                lowestPlayer = activePlayer;
            }
        }

        return found;
    }

    void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input) { }
    void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner) { }
    void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    void INetworkRunnerCallbacks.OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner) { }
    void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner) { }
}
