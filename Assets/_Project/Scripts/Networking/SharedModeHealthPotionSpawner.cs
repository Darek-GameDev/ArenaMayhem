using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class SharedModeHealthPotionSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("References")]
    [SerializeField] private NetworkRunner runner;
    [SerializeField] private NetworkObject potionPrefab;

    [Header("Spawn Rules")]
    [SerializeField] private bool spawnOnFirstPlayerJoin = true;
    [SerializeField] private bool allowSpawnWithF = true;
    [SerializeField] private bool onlyOnePotionAtATime = true;
    [SerializeField] private Transform[] randomSpawnPoints;
    [SerializeField] private Transform fallbackSpawnPoint;
    [SerializeField] private bool logSpawn = true;

    private bool callbacksRegistered;
    private NetworkObject spawnedPotion;

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

        if (spawnedPotion == null)
        {
            spawnedPotion = FindExistingPotionObject();
        }

        if (!allowSpawnWithF || runner == null)
        {
            return;
        }

        if (runner.GameMode != GameMode.Shared || !runner.LocalPlayer.IsRealPlayer)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.fKey.wasPressedThisFrame)
        {
            return;
        }

        TrySpawnPotion(runner, requireFirstRealPlayer: false);
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
        if (player != runner.LocalPlayer || !spawnOnFirstPlayerJoin)
        {
            return;
        }

        TrySpawnPotion(runner, requireFirstRealPlayer: true);
    }

    void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (spawnedPotion == null)
        {
            return;
        }

        if (!spawnedPotion.HasStateAuthority)
        {
            return;
        }

        if (GetRealPlayerCount(runner) > 0)
        {
            return;
        }

        runner.Despawn(spawnedPotion);
        spawnedPotion = null;
    }

    private void TrySpawnPotion(NetworkRunner currentRunner, bool requireFirstRealPlayer)
    {
        if (potionPrefab == null || currentRunner == null)
        {
            return;
        }

        if (currentRunner.GameMode != GameMode.Shared)
        {
            return;
        }

        if (requireFirstRealPlayer && GetRealPlayerCount(currentRunner) != 1)
        {
            return;
        }

        if (spawnedPotion == null)
        {
            spawnedPotion = FindExistingPotionObject();
        }

        if (onlyOnePotionAtATime && spawnedPotion != null)
        {
            return;
        }

        Vector3 spawnPosition = GetRandomSpawnPosition(out Quaternion spawnRotation);
        spawnedPotion = currentRunner.Spawn(potionPrefab, spawnPosition, spawnRotation, currentRunner.LocalPlayer);

        if (logSpawn && spawnedPotion != null)
        {
            Debug.Log($"SharedModeHealthPotionSpawner: spawned potion '{spawnedPotion.name}' at {spawnPosition}.");
        }
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

    private static NetworkObject FindExistingPotionObject()
    {
        SharedModeHealthPotionItem[] items = FindObjectsByType<SharedModeHealthPotionItem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null && items[i].Object != null)
            {
                return items[i].Object;
            }
        }

        return null;
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
