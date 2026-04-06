using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

[DisallowMultipleComponent]
public class SharedModeEnemySpawnCallbacks : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkRunner runner;
    [SerializeField] private NetworkObject enemyPrefab;
    [SerializeField] private Transform[] randomSpawnPoints;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private bool logSpawn = true;

    private bool callbacksRegistered;
    private NetworkObject spawnedEnemy;

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

        if (spawnedEnemy == null)
        {
            spawnedEnemy = FindExistingEnemyObject();
        }
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
        if (player != runner.LocalPlayer)
        {
            return;
        }

        TrySpawnEnemy(runner, requireFirstRealPlayer: true);
    }

    void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (spawnedEnemy == null)
        {
            return;
        }

        if (!spawnedEnemy.HasStateAuthority)
        {
            return;
        }

        if (GetRealPlayerCount(runner) > 0)
        {
            return;
        }

        runner.Despawn(spawnedEnemy);
        spawnedEnemy = null;
    }

    private void TrySpawnEnemy(NetworkRunner currentRunner, bool requireFirstRealPlayer)
    {
        if (enemyPrefab == null || currentRunner == null)
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

        if (spawnedEnemy == null)
        {
            spawnedEnemy = FindExistingEnemyObject();
        }

        if (spawnedEnemy != null)
        {
            return;
        }

        Vector3 spawnPosition = GetEnemySpawnPosition(out Quaternion spawnRotation);

        spawnedEnemy = currentRunner.Spawn(enemyPrefab, spawnPosition, spawnRotation, currentRunner.LocalPlayer);

        if (logSpawn && spawnedEnemy != null)
        {
            Debug.Log($"SharedModeEnemySpawnCallbacks: spawned enemy '{spawnedEnemy.name}' at {spawnPosition}.");
        }
    }

    private Vector3 GetEnemySpawnPosition(out Quaternion rotation)
    {
        if (TryGetRandomSpawnPoint(randomSpawnPoints, out Transform randomPoint))
        {
            rotation = randomPoint.rotation;
            return randomPoint.position;
        }

        if (spawnPoint != null)
        {
            rotation = spawnPoint.rotation;
            return spawnPoint.position;
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

    private static int GetRealPlayerCount(NetworkRunner runner)
    {
        int count = 0;
        foreach (PlayerRef activePlayer in runner.ActivePlayers)
        {
            if (activePlayer.IsRealPlayer)
            {
                count++;
            }
        }

        return count;
    }

    private static NetworkObject FindExistingEnemyObject()
    {
        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null && enemies[i].Object != null)
            {
                return enemies[i].Object;
            }
        }

        return null;
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
