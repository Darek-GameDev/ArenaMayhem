using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkCharacterController))]
public class SharedModePlayerController : NetworkBehaviour
{
    public enum LocomotionState : byte
    {
        Idle = 0,
        Moving = 1,
        Sprinting = 2,
        Jumping = 3,
        Falling = 4,
    }

    public enum CombatState : byte
    {
        None = 0,
        Attacking = 1,
        Blocking = 2,
        BlockHit = 3,
    }

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float runSpeed = 5f;
    [SerializeField] private float lookYawSpeed = 0.15f;
    [SerializeField] private float instantAcceleration = 100f;
    [SerializeField] private float instantBraking = 100f;
    [SerializeField] private Transform cameraTransform;

    [Header("Combat")]
    [SerializeField] private float blockCooldownSeconds = 0.25f;

    [Header("Health")]
    [SerializeField] private int maxHealth = 10;

    [Networked] public int Health { get; set; }
    [Networked] public NetworkBool IsDead { get; set; }
    [Networked] public NetworkBool IsBlocking { get; set; }
    [Networked] public NetworkBool AttackPressed { get; set; }
    [Networked] public byte ComboStep { get; set; }
    [Networked] public int HitSequence { get; set; }
    [Networked] public LocomotionState NetLocomotionState { get; set; }
    [Networked] public CombatState NetCombatState { get; set; }
    [Networked] public float NextBlockAllowedAt { get; set; }

    private NetworkCharacterController cc;

    public override void Spawned()
    {
        cc = GetComponent<NetworkCharacterController>();
        cc.acceleration = instantAcceleration;
        cc.braking = instantBraking;
        cc.rotationSpeed = 0f;

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (HasStateAuthority)
        {
            Health = maxHealth;
            IsDead = false;
            IsBlocking = false;
            AttackPressed = false;
            ComboStep = 0;
            HitSequence = 0;
            NetLocomotionState = LocomotionState.Idle;
            NetCombatState = CombatState.None;
            NextBlockAllowedAt = 0f;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
        {
            return;
        }

        if (IsDead)
        {
            NetLocomotionState = LocomotionState.Idle;
            NetCombatState = CombatState.None;
            IsBlocking = false;
            AttackPressed = false;
            return;
        }

        if (!GetInput(out PlayerNetworkInput input))
        {
            return;
        }

        float yaw = input.Look.x * lookYawSpeed;
        if (Mathf.Abs(yaw) > 0.0001f)
        {
            transform.Rotate(0f, yaw, 0f);
        }

        bool sprintHeld = input.Buttons.IsSet((int)PlayerInputButton.Sprint);
        bool blockHeld = input.Buttons.IsSet((int)PlayerInputButton.Block);

        cc.maxSpeed = sprintHeld ? runSpeed : walkSpeed;

        Vector3 worldDirection = GetWorldMoveDirection(input.Move);
        cc.Move(worldDirection);

        if (worldDirection.sqrMagnitude <= 0.0001f)
        {
            Vector3 velocity = cc.Velocity;
            velocity.x = 0f;
            velocity.z = 0f;
            cc.Velocity = velocity;
        }

        if (input.JumpPressed)
        {
            cc.Jump();
        }

        float simTime = (float)Runner.SimulationTime;

        // Drive block from held state to avoid dropped pressed/released transitions.
        if (blockHeld)
        {
            if (!IsBlocking && simTime >= NextBlockAllowedAt)
            {
                IsBlocking = true;
                NetCombatState = CombatState.Blocking;
                ComboStep = 0;
            }
        }
        else if (IsBlocking)
        {
            IsBlocking = false;
            NextBlockAllowedAt = simTime + blockCooldownSeconds;
            if (NetCombatState == CombatState.Blocking || NetCombatState == CombatState.BlockHit)
            {
                NetCombatState = CombatState.None;
            }
        }

        if (input.AttackPressed && !IsBlocking)
        {
            AttackPressed = true;
            NetCombatState = CombatState.Attacking;
            ComboStep = (byte)((ComboStep + 1) % 4);
        }
        else
        {
            AttackPressed = false;
            if (!IsBlocking && NetCombatState == CombatState.Attacking)
            {
                NetCombatState = CombatState.None;
            }
        }

        UpdateLocomotionState(worldDirection, sprintHeld);

        if (worldDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(worldDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 20f * Runner.DeltaTime);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestDamage(int amount)
    {
        if (amount <= 0 || IsDead)
        {
            return;
        }

        HitSequence++;

        if (IsBlocking)
        {
            NetCombatState = CombatState.BlockHit;
            return;
        }

        Health = Mathf.Max(0, Health - amount);
        if (Health == 0)
        {
            IsDead = true;
            IsBlocking = false;
            NetCombatState = CombatState.None;
            NetLocomotionState = LocomotionState.Idle;
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ResetAfterRespawn(Vector3 worldPosition)
    {
        transform.position = worldPosition;

        if (HasStateAuthority)
        {
            Health = maxHealth;
            IsDead = false;
            IsBlocking = false;
            AttackPressed = false;
            ComboStep = 0;
            HitSequence = 0;
            NetLocomotionState = LocomotionState.Idle;
            NetCombatState = CombatState.None;
            NextBlockAllowedAt = 0f;
        }
    }

    private void UpdateLocomotionState(Vector3 worldDirection, bool sprintHeld)
    {
        if (!cc.Grounded)
        {
            NetLocomotionState = cc.Velocity.y >= 0f ? LocomotionState.Jumping : LocomotionState.Falling;
            return;
        }

        if (worldDirection.sqrMagnitude <= 0.0001f)
        {
            NetLocomotionState = LocomotionState.Idle;
            return;
        }

        NetLocomotionState = sprintHeld ? LocomotionState.Sprinting : LocomotionState.Moving;
    }

    private Vector3 GetWorldMoveDirection(Vector2 inputMove)
    {
        Vector3 localMove = new Vector3(inputMove.x, 0f, inputMove.y);
        if (localMove.sqrMagnitude > 1f)
        {
            localMove.Normalize();
        }

        Transform reference = cameraTransform;
        if (reference == null && Camera.main != null)
        {
            reference = Camera.main.transform;
        }

        if (reference == null)
        {
            return transform.forward * localMove.z + transform.right * localMove.x;
        }

        Vector3 forward = reference.forward;
        Vector3 right = reference.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();
        return forward * localMove.z + right * localMove.x;
    }
}
