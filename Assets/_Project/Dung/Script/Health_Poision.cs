using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class Health_Poision : NetworkBehaviour
{
    [SerializeField] private int healAmount = 10;
    [SerializeField] private bool destroyOnCollect = true;
    [SerializeField] private bool logCollect = false;

    [Networked] private NetworkBool IsCollected { get; set; }

    private bool hasSpawned;
    private Collider itemCollider;

    private void Awake()
    {
        itemCollider = GetComponent<Collider>();
    }

    public override void Spawned()
    {
        hasSpawned = true;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        hasSpawned = false;
    }

    private void Reset()
    {
        Collider c = GetComponent<Collider>();
        if (c != null)
        {
            c.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryCollect(other);
    }

    private void TryCollect(Collider other)
    {
        if (!hasSpawned || IsCollected)
        {
            return;
        }

        SharedModePlayerController collector = other.GetComponentInParent<SharedModePlayerController>();
        if (collector == null || collector.Object == null || !collector.Object.IsValid)
        {
            return;
        }

        if (collector.IsDead)
        {
            return;
        }

        if (Object == null || !Object.IsValid)
        {
            return;
        }

        PlayerRef collectorRef = collector.Object.InputAuthority;
        if (HasStateAuthority)
        {
            ApplyCollect(collector);
        }
        else
        {
            RPC_RequestCollect(collectorRef);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestCollect(PlayerRef collectorRef)
    {
        if (!hasSpawned || IsCollected)
        {
            return;
        }

        SharedModePlayerController collector = ResolveCollector(collectorRef);
        if (collector == null || collector.IsDead)
        {
            return;
        }

        ApplyCollect(collector);
    }

    private SharedModePlayerController ResolveCollector(PlayerRef collectorRef)
    {
        if (Runner == null || !collectorRef.IsRealPlayer)
        {
            return null;
        }

        NetworkObject playerObject = Runner.GetPlayerObject(collectorRef);
        if (playerObject == null)
        {
            return null;
        }

        return playerObject.GetComponent<SharedModePlayerController>();
    }

    private void ApplyCollect(SharedModePlayerController collector)
    {
        if (IsCollected)
        {
            return;
        }

        IsCollected = true;

        // Cộng máu cho player
        collector.RPC_RequestAddHealth(healAmount);

        if (logCollect)
        {
            Debug.Log($"{name}: collected by {collector.name}. +{healAmount} HP");
        }

        if (destroyOnCollect && Runner != null && Object != null && Object.IsValid)
        {
            Runner.Despawn(Object);
        }
    }
}
