using Fusion;
using UnityEngine;

public enum PlayerInputButton
{
    Jump = 0,
    Sprint = 1,
    Attack = 2,
    Block = 3,
    Aim = 4,
}

public struct PlayerNetworkInput : INetworkInput
{
    public Vector2 Move;
    public Vector2 Look;
    public NetworkButtons Buttons;

    // Explicit transitions help deterministic state changes in FixedUpdateNetwork.
    public NetworkBool JumpPressed;
    public NetworkBool JumpReleased;
    public NetworkBool SprintPressed;
    public NetworkBool SprintReleased;
    public NetworkBool AttackPressed;
    public NetworkBool AttackReleased;
    public NetworkBool BlockPressed;
    public NetworkBool BlockReleased;
    public NetworkBool AimPressed;
    public NetworkBool AimReleased;
}
