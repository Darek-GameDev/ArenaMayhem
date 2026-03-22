using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerInputButton
{
    Jump = 0,
    Run = 1,
    Attack = 2,
    Block = 3
}

public struct PlayerNetworkInputData : INetworkInput
{
    public Vector2 Move;
    public NetworkButtons Buttons;
}

public class NetworkInput : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkRunner runner;
    [SerializeField] private bool autoFindRunner = true;

    private bool isRegistered;

    private void OnEnable()
    {
        TryRegisterCallbacks();
    }

    private void Update()
    {
        if (isRegistered)
        {
            return;
        }

        TryRegisterCallbacks();
    }
//ư
    private void OnDisable()
    {
        if (runner == null || !isRegistered)
        {
            return;
        }

        runner.RemoveCallbacks(this);
        isRegistered = false;
    }

    private void TryRegisterCallbacks()
    {
        if (runner == null && autoFindRunner)
        {
            runner = FindObjectOfType<NetworkRunner>();
        }

        if (runner == null || isRegistered)
        {
            return;
        }

        runner.ProvideInput = true;
        runner.AddCallbacks(this);
        isRegistered = true;
    }

    public void OnInput(NetworkRunner runner, Fusion.NetworkInput input)
    {
        PlayerNetworkInputData data = new PlayerNetworkInputData
        {
            Move = ReadMoveInput(),
            Buttons = ReadButtons()
        };

        input.Set(data);
    }

    private static Vector2 ReadMoveInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return Vector2.zero;
        }

        float x = 0f;
        float y = 0f;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            x -= 1f;
        }

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            x += 1f;
        }

        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
        {
            y -= 1f;
        }

        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
        {
            y += 1f;
        }

        return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
    }
    private static NetworkButtons ReadButtons()
    {
        NetworkButtons buttons = default;
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (keyboard != null)
        {
            bool runPressed = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            buttons.Set((int)PlayerInputButton.Run, runPressed);
            buttons.Set((int)PlayerInputButton.Jump, keyboard.spaceKey.isPressed);
        }

        if (mouse != null)
        {
            buttons.Set((int)PlayerInputButton.Attack, mouse.leftButton.isPressed);
            buttons.Set((int)PlayerInputButton.Block, mouse.rightButton.isPressed);
        }

        return buttons;
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, Fusion.NetworkInput input)
    {
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
    }
}