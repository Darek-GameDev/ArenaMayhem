using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class SharedModeRunnerCallbacks : MonoBehaviour, INetworkRunnerCallbacks
{
    private const string GameplaySceneName = "MainMap";

    [Header("References")]
    [SerializeField] private NetworkRunner runner;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private NetworkObject swordPrefab;
    [SerializeField] private NetworkObject archerPrefab;
    [SerializeField] private Transform[] spawnPoints;

    [Header("Look Sensitivity")]
    [SerializeField] private float normalLookSensitivity = 1f;
    [SerializeField] private float aimLookSensitivity = 0.55f;

    private readonly Dictionary<PlayerRef, NetworkObject> spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();
    private bool callbacksRegistered;
    private bool spawnRequestInProgress;

    private bool prevJump;
    private bool prevSprint;
    private bool prevAttack;
    private bool prevAttackSecondary;
    private bool prevWeaponSlot1;
    private bool prevWeaponSlot2;
    private bool prevBlock;
    private bool prevAim;
    private bool prevSkill;
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
        RequestSpawnLocalPlayer();
    }

    private void Update()
    {
        if (!callbacksRegistered)
        {
            TryResolveReferences();
            TryRegisterCallbacks();
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

        RequestSpawnLocalPlayer();
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

        bool jumpHeld = ReadButtonHeld("Jump");
        bool sprintHeld = ReadButtonHeld("Sprint");
        bool attackHeld = ReadButtonHeld("Attack");
        bool attackSecondaryHeld = false;
        bool weaponSlot1Held = ReadButtonHeld("Previous");
        bool weaponSlot2Held = ReadButtonHeld("Next");
        bool blockHeld = ReadButtonHeld("Block");
        bool aimHeld = ReadButtonHeld("Aim");
        bool skillHeld = ReadButtonHeld("Skill");

        Vector2 move = ReadVector2("Move");
        Vector2 look = ApplyLookSensitivity(ReadVector2("Look"), aimHeld);

        NetworkButtons buttons = default;
        buttons.Set((int)PlayerInputButton.Jump, jumpHeld);
        buttons.Set((int)PlayerInputButton.Sprint, sprintHeld);
        buttons.Set((int)PlayerInputButton.Attack, attackHeld);
        buttons.Set((int)PlayerInputButton.AttackSecondary, attackSecondaryHeld);
        buttons.Set((int)PlayerInputButton.WeaponSlot1, weaponSlot1Held);
        buttons.Set((int)PlayerInputButton.WeaponSlot2, weaponSlot2Held);
        buttons.Set((int)PlayerInputButton.Block, blockHeld);
        buttons.Set((int)PlayerInputButton.Aim, aimHeld);
        buttons.Set((int)PlayerInputButton.Skill, skillHeld);

        ComputeTransitions(jumpHeld, ref prevJump, out NetworkBool jumpPressed, out NetworkBool jumpReleased);
        ComputeTransitions(sprintHeld, ref prevSprint, out NetworkBool sprintPressed, out NetworkBool sprintReleased);
        ComputeTransitions(attackHeld, ref prevAttack, out NetworkBool attackPressed, out NetworkBool attackReleased);
        ComputeTransitions(attackSecondaryHeld, ref prevAttackSecondary, out NetworkBool attackSecondaryPressed, out NetworkBool attackSecondaryReleased);
        ComputeTransitions(weaponSlot1Held, ref prevWeaponSlot1, out NetworkBool weaponSlot1Pressed, out NetworkBool weaponSlot1Released);
        ComputeTransitions(weaponSlot2Held, ref prevWeaponSlot2, out NetworkBool weaponSlot2Pressed, out NetworkBool weaponSlot2Released);
        ComputeTransitions(blockHeld, ref prevBlock, out NetworkBool blockPressed, out NetworkBool blockReleased);
        ComputeTransitions(aimHeld, ref prevAim, out NetworkBool aimPressed, out NetworkBool aimReleased);
        ComputeTransitions(skillHeld, ref prevSkill, out NetworkBool skillPressed, out NetworkBool skillReleased);

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
            AttackSecondaryPressed = attackSecondaryPressed,
            AttackSecondaryReleased = attackSecondaryReleased,
            WeaponSlot1Pressed = weaponSlot1Pressed,
            WeaponSlot1Released = weaponSlot1Released,
            WeaponSlot2Pressed = weaponSlot2Pressed,
            WeaponSlot2Released = weaponSlot2Released,
            BlockPressed = blockPressed,
            BlockReleased = blockReleased,
            AimPressed = aimPressed,
            AimReleased = aimReleased,
            SkillPressed = skillPressed,
            SkillReleased = skillReleased,
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
        if (runner.GameMode != GameMode.Shared || player != runner.LocalPlayer)
        {
            return;
        }

        TrySpawnLocalPlayer(runner);
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

    }

    private static SharedPlayerClassType ResolveSelectedClass(NetworkRunner sourceRunner, PlayerRef player)
    {
        if (SharedRoomSessionManager.TryGetPlayerClass(sourceRunner, player, out SharedPlayerClassType networkClass))
        {
            return networkClass;
        }

        SharedPlayerClassType fallbackClass = SharedPlayerClassTypeUtility.FromClassName(ClassChoose.LastConfirmedClassName);
        if (fallbackClass != SharedPlayerClassType.Unknown)
        {
            return fallbackClass;
        }

        return SharedPlayerClassType.Knight;
    }

    private Vector3 GetSpawnPosition()
    {
        if (TryGetRandomSpawnPoint(spawnPoints, out Transform point))
        {
            return point.position;
        }

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

    private Vector2 ApplyLookSensitivity(Vector2 lookInput, bool aimHeld)
    {
        float sensitivity = aimHeld ? aimLookSensitivity : normalLookSensitivity;
        return lookInput * Mathf.Max(0f, sensitivity);
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
            case "Previous":
                return keyboard != null && keyboard.digit1Key.isPressed;
            case "Next":
                return keyboard != null && keyboard.digit2Key.isPressed;
            case "Block":
                return mouse != null && mouse.rightButton.isPressed;
            case "Skill":
                return keyboard != null && keyboard.eKey.isPressed;
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

        if (playerInput != null)
        {
            if (!playerInput.enabled)
            {
                playerInput.enabled = true;
            }

            TrySwitchToPlayerActionMap();
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
    void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner)
    {
        RequestSpawnLocalPlayer(runner);
    }

    private void RequestSpawnLocalPlayer()
    {
        if (runner == null)
        {
            return;
        }

        RequestSpawnLocalPlayer(runner);
    }

    private void RequestSpawnLocalPlayer(NetworkRunner currentRunner)
    {
        if (currentRunner == null || spawnRequestInProgress)
        {
            return;
        }

        StartCoroutine(SpawnLocalPlayerNextFrame(currentRunner));
    }

    private System.Collections.IEnumerator SpawnLocalPlayerNextFrame(NetworkRunner currentRunner)
    {
        spawnRequestInProgress = true;
        yield return null;

        if (!string.Equals(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, GameplaySceneName, StringComparison.OrdinalIgnoreCase))
        {
            spawnRequestInProgress = false;
            yield break;
        }

        TrySpawnLocalPlayer(currentRunner);
        spawnRequestInProgress = false;
    }

    private void TrySpawnLocalPlayer(NetworkRunner currentRunner)
    {
        if (currentRunner == null || currentRunner.GameMode != GameMode.Shared || !currentRunner.LocalPlayer.IsRealPlayer)
        {
            return;
        }

        PlayerRef localPlayer = currentRunner.LocalPlayer;
        if (spawnedPlayers.ContainsKey(localPlayer))
        {
            return;
        }

        NetworkObject existingPlayerObject = currentRunner.GetPlayerObject(localPlayer);
        if (existingPlayerObject != null)
        {
            spawnedPlayers[localPlayer] = existingPlayerObject;
            CacheLocalPlayerInput(existingPlayerObject);
            return;
        }

        SharedPlayerClassType selectedClass = ResolveSelectedClass(currentRunner, localPlayer);
        bool isSwordPlayer = SharedPlayerClassTypeUtility.IsSwordClass(selectedClass);
        NetworkObject prefabToSpawn = isSwordPlayer ? swordPrefab : archerPrefab;

        if (prefabToSpawn == null)
        {
            Debug.LogWarning($"SharedModeRunnerCallbacks: missing {(isSwordPlayer ? "swordPrefab" : "archerPrefab")}.");
            return;
        }

        Vector3 spawnPosition = GetSpawnPosition();
        Quaternion spawnRotation = Quaternion.identity;

        NetworkObject spawned = currentRunner.Spawn(prefabToSpawn, spawnPosition, spawnRotation, localPlayer);
        if (spawned == null)
        {
            return;
        }

        spawnedPlayers[localPlayer] = spawned;
        currentRunner.SetPlayerObject(localPlayer, spawned);

        SharedModePlayerController playerController = spawned.GetComponent<SharedModePlayerController>();
        if (playerController != null)
        {
            playerController.ApplySpawnClass(selectedClass);
        }

        CacheLocalPlayerInput(spawned);
    }

    private void CacheLocalPlayerInput(NetworkObject playerObject)
    {
        if (playerObject == null)
        {
            return;
        }

        PlayerInput localInput = playerObject.GetComponent<PlayerInput>();
        if (localInput == null)
        {
            localInput = playerObject.GetComponentInChildren<PlayerInput>(true);
        }

        if (localInput == null)
        {
            return;
        }

        playerInput = localInput;
        warnedMissingInput = false;
        if (!playerInput.enabled)
        {
            playerInput.enabled = true;
        }

        TrySwitchToPlayerActionMap();
    }

    private void TrySwitchToPlayerActionMap()
    {
        if (playerInput == null || !playerInput.isActiveAndEnabled)
        {
            return;
        }

        if (playerInput.currentActionMap == null || playerInput.currentActionMap.name != "Player")
        {
            playerInput.SwitchCurrentActionMap("Player");
        }
    }
}
