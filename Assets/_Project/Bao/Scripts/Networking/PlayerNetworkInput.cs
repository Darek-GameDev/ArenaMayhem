using Fusion;
using UnityEngine;

public enum PlayerInputButton
{
    Jump = 0,
    Sprint = 1,
    Attack = 2,
    AttackSecondary = 3,
    Block = 4,
    Aim = 5,
    WeaponSlot1 = 6,
    WeaponSlot2 = 7,
    Skill = 8,
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
    public NetworkBool AttackSecondaryPressed;
    public NetworkBool AttackSecondaryReleased;
    public NetworkBool WeaponSlot1Pressed;
    public NetworkBool WeaponSlot1Released;
    public NetworkBool WeaponSlot2Pressed;
    public NetworkBool WeaponSlot2Released;
    public NetworkBool BlockPressed;
    public NetworkBool BlockReleased;
    public NetworkBool AimPressed;
    public NetworkBool AimReleased;
    public NetworkBool SkillPressed;
    public NetworkBool SkillReleased;
}
