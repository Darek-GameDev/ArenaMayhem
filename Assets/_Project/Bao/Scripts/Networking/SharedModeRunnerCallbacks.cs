using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class SharedModeRunnerCallbacks : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("References")]
    [SerializeField] private NetworkRunner runner;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;

    [Header("Enemy Spawn")]
    [SerializeField] private bool spawnEnemyOnFirstJoin = true;
    [SerializeField] private NetworkObject enemyPrefab;
    [SerializeField] private Transform[] enemySpawnPoints;
    [SerializeField] private Transform enemySpawnPoint;
    [SerializeField] private bool logEnemySpawn = true;

    private readonly Dictionary<PlayerRef, NetworkObject> spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();
    private NetworkObject spawnedEnemy;
    private bool callbacksRegistered;

    private bool prevJump;
    private bool prevSprint;
    private bool prevAttack;
    private bool prevBlock;
    private bool prevAim;
    private bool warnedMissingInput;

    private void Awake()
    {
        TryResolveReferences();
        TryRegisterCallbacks();
    }

    private void OnEnable()
    {
        TryResolveReferences();
        TryRegisterCallbacks();
    }

    private void Update()
    {
        if (!callbacksRegistered)
        {
            TryResolveReferences();
            TryRegisterCallbacks();
        }

        if (spawnEnemyOnFirstJoin && spawnedEnemy == null)
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

    private void TryResolveReferences()
    {
        if (runner == null)
        {
            runner = GetComponent<NetworkRunner>();
        }

        if (runner == null)
        {
            runner = FindFirstObjectByType<NetworkRunner>();
        }

        if (playerInput == null)
        {
            playerInput = FindFirstObjectByType<PlayerInput>();
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

    void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input)
    {
        ResolveLocalPlayerInput(runner);

        Vector2 move = ReadVector2("Move");
        Vector2 look = ReadVector2("Look");

        bool jumpHeld = ReadButtonHeld("Jump");
        bool sprintHeld = ReadButtonHeld("Sprint");
        bool attackHeld = ReadButtonHeld("Attack");
        bool blockHeld = ReadButtonHeld("Block");
        bool aimHeld = ReadButtonHeld("Aim");

        NetworkButtons buttons = default;
        buttons.Set((int)PlayerInputButton.Jump, jumpHeld);
        buttons.Set((int)PlayerInputButton.Sprint, sprintHeld);
        buttons.Set((int)PlayerInputButton.Attack, attackHeld);
        buttons.Set((int)PlayerInputButton.Block, blockHeld);
        buttons.Set((int)PlayerInputButton.Aim, aimHeld);

        ComputeTransitions(jumpHeld, ref prevJump, out NetworkBool jumpPressed, out NetworkBool jumpReleased);
        ComputeTransitions(sprintHeld, ref prevSprint, out NetworkBool sprintPressed, out NetworkBool sprintReleased);
        ComputeTransitions(attackHeld, ref prevAttack, out NetworkBool attackPressed, out NetworkBool attackReleased);
        ComputeTransitions(blockHeld, ref prevBlock, out NetworkBool blockPressed, out NetworkBool blockReleased);
        ComputeTransitions(aimHeld, ref prevAim, out NetworkBool aimPressed, out NetworkBool aimReleased);

        PlayerNetworkInput data = new PlayerNetworkInput
        {
            Move = Vector2.ClampMagnitude(move, 1f),
            Look = look,
            Buttons = buttons,
            JumpPressed = jumpPressed,
            JumpReleased = jumpReleased,
            SprintPressed = sprintPressed,
            SprintReleased = sprintReleased,
            AttackPressed = attackPressed,
            AttackReleased = attackReleased,
            BlockPressed = blockPressed,
            BlockReleased = blockReleased,
            AimPressed = aimPressed,
            AimReleased = aimReleased,
        };

        input.Set(data);

        if (!warnedMissingInput && ShouldWarnMissingPlayerInput(runner))
        {
            warnedMissingInput = true;
            Debug.LogWarning("SharedModeRunnerCallbacks: khong tim thay PlayerInput local. Dang dung fallback keyboard/mouse.");
        }
    }

    void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (playerPrefab == null)
        {
            Debug.LogWarning("SharedModeRunnerCallbacks: missing playerPrefab.");
            return;
        }

        if (runner.GameMode != GameMode.Shared || player != runner.LocalPlayer)
        {
            return;
        }

        if (spawnedPlayers.ContainsKey(player))
        {
            return;
        }

        Vector3 spawnPosition = GetSpawnPosition();
        Quaternion spawnRotation = Quaternion.identity;

        NetworkObject spawned = runner.Spawn(playerPrefab, spawnPosition, spawnRotation, player);
        if (spawned != null)
        {
            spawnedPlayers[player] = spawned;
            runner.SetPlayerObject(player, spawned);

            if (player == runner.LocalPlayer)
            {
                PlayerInput localInput = spawned.GetComponent<PlayerInput>();
                if (localInput == null)
                {
                    localInput = spawned.GetComponentInChildren<PlayerInput>(true);
                }

                if (localInput != null)
                {
                    playerInput = localInput;
                    warnedMissingInput = false;
                    if (playerInput.currentActionMap == null || playerInput.currentActionMap.name != "Player")
                    {
                        playerInput.SwitchCurrentActionMap("Player");
                    }
                }
            }
        }

        TrySpawnEnemyOnFirstJoin(runner, player);
    }

    void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (!spawnedPlayers.TryGetValue(player, out NetworkObject obj))
        {
            return;
        }

        if (obj != null && obj.HasStateAuthority)
        {
            runner.Despawn(obj);
        }

        spawnedPlayers.Remove(player);

        TryDespawnEnemyIfNoPlayers(runner);
    }

    private void TrySpawnEnemyOnFirstJoin(NetworkRunner currentRunner, PlayerRef joinedPlayer)
    {
        if (!spawnEnemyOnFirstJoin || enemyPrefab == null || currentRunner == null)
        {
            return;
        }

        if (currentRunner.GameMode != GameMode.Shared)
        {
            return;
        }

        if (joinedPlayer != currentRunner.LocalPlayer)
        {
            return;
        }

        if (GetRealPlayerCount(currentRunner) != 1)
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

        if (logEnemySpawn && spawnedEnemy != null)
        {
            Debug.Log($"SharedModeRunnerCallbacks: spawned enemy '{spawnedEnemy.name}' at {spawnPosition}.");
        }
    }

    private void TryDespawnEnemyIfNoPlayers(NetworkRunner currentRunner)
    {
        if (spawnedEnemy == null || currentRunner == null)
        {
            return;
        }

        if (GetRealPlayerCount(currentRunner) > 0)
        {
            return;
        }

        if (spawnedEnemy.HasStateAuthority)
        {
            currentRunner.Despawn(spawnedEnemy);
        }

        spawnedEnemy = null;
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

    private Vector3 GetSpawnPosition()
    {
        if (TryGetRandomSpawnPoint(spawnPoints, out Transform point))
        {
            return point.position;
        }

        return Vector3.zero;
    }

    private Vector3 GetEnemySpawnPosition(out Quaternion rotation)
    {
        if (TryGetRandomSpawnPoint(enemySpawnPoints, out Transform randomEnemyPoint))
        {
            rotation = randomEnemyPoint.rotation;
            return randomEnemyPoint.position;
        }

        if (enemySpawnPoint != null)
        {
            rotation = enemySpawnPoint.rotation;
            return enemySpawnPoint.position;
        }

        if (TryGetRandomSpawnPoint(spawnPoints, out Transform fallbackPlayerPoint))
        {
            rotation = fallbackPlayerPoint.rotation;
            return fallbackPlayerPoint.position;
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

    private Vector2 ReadVector2(string actionName)
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (actionName == "Look" && mouse != null)
        {
            return mouse.delta.ReadValue();
        }

        InputAction action = playerInput != null ? playerInput.actions[actionName] : null;
        if (action != null)
        {
            return action.ReadValue<Vector2>();
        }

        if (actionName == "Move" && keyboard != null)
        {
            Vector2 move = Vector2.zero;
            if (keyboard.wKey.isPressed) move.y += 1f;
            if (keyboard.sKey.isPressed) move.y -= 1f;
            if (keyboard.dKey.isPressed) move.x += 1f;
            if (keyboard.aKey.isPressed) move.x -= 1f;
            return move;
        }

        return Vector2.zero;
    }

    private bool ReadButtonHeld(string actionName)
    {
        InputAction action = playerInput != null ? playerInput.actions[actionName] : null;
        if (action != null)
        {
            return action.IsPressed();
        }

        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        switch (actionName)
        {
            case "Jump":
                return keyboard != null && keyboard.spaceKey.isPressed;
            case "Sprint":
                return keyboard != null && keyboard.leftShiftKey.isPressed;
            case "Attack":
                return mouse != null && mouse.leftButton.isPressed;
            case "Block":
                return mouse != null && mouse.rightButton.isPressed;
            default:
                return false;
        }
    }

    private void ResolveLocalPlayerInput(NetworkRunner currentRunner)
    {
        if (playerInput != null && playerInput.isActiveAndEnabled)
        {
            return;
        }

        if (currentRunner != null && currentRunner.LocalPlayer.IsRealPlayer)
        {
            NetworkObject localPlayerObject = currentRunner.GetPlayerObject(currentRunner.LocalPlayer);
            if (localPlayerObject != null)
            {
                PlayerInput fromPlayerObject = localPlayerObject.GetComponent<PlayerInput>();
                if (fromPlayerObject == null)
                {
                    fromPlayerObject = localPlayerObject.GetComponentInChildren<PlayerInput>(true);
                }

                if (fromPlayerObject != null)
                {
                    playerInput = fromPlayerObject;
                    warnedMissingInput = false;
                }
            }
        }

        if (playerInput == null)
        {
            playerInput = FindFirstObjectByType<PlayerInput>();
        }

        if (playerInput != null && (playerInput.currentActionMap == null || playerInput.currentActionMap.name != "Player"))
        {
            playerInput.SwitchCurrentActionMap("Player");
        }
    }

    private bool ShouldWarnMissingPlayerInput(NetworkRunner currentRunner)
    {
        if (playerInput != null)
        {
            return false;
        }

        if (currentRunner == null || !currentRunner.LocalPlayer.IsRealPlayer)
        {
            return false;
        }

        return currentRunner.GetPlayerObject(currentRunner.LocalPlayer) != null;
    }

    private static void ComputeTransitions(bool current, ref bool previous, out NetworkBool pressed, out NetworkBool released)
    {
        pressed = current && !previous;
        released = !current && previous;
        previous = current;
    }

    void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
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
