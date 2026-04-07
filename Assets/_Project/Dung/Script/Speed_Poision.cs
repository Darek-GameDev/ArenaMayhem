using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class Speed_Poision : NetworkBehaviour
{
    [SerializeField] private float speedAmount = 10f;
    [SerializeField] private float buffDurationSeconds = 3f;
    [SerializeField] private bool destroyOnCollect = true;
    [SerializeField] private bool logCollect = false;

    [Networked] private NetworkBool IsCollected { get; set; }

    private bool hasSpawned;

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
        collector.RPC_RequestAddSpeed(speedAmount, buffDurationSeconds);

        if (logCollect)
        {
            Debug.Log($"{name}: collected by {collector.name}. +{speedAmount} speed for {buffDurationSeconds}s");
        }

        if (destroyOnCollect && Runner != null && Object != null && Object.IsValid)
        {
            Runner.Despawn(Object);
        }
    }
}
