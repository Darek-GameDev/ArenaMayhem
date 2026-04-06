using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class SharedModeHealthPotionItem : NetworkBehaviour
{
    [SerializeField] private bool healCollectorToFull = true;
    [SerializeField] private bool destroyOnCollect = true;
    [SerializeField] private bool logCollect = false;
    [SerializeField] private Renderer[] itemRenderers;

    [Networked] private NetworkBool IsCollected { get; set; }

    private bool hasSpawned;
    private bool collectInProgress;
    private bool visualStateInitialized;
    private bool lastVisualCollected;
    private Collider itemCollider;

    private void Awake()
    {
        itemCollider = GetComponent<Collider>();

        if (itemRenderers == null || itemRenderers.Length == 0)
        {
            itemRenderers = GetComponentsInChildren<Renderer>(true);
        }
    }

    public override void Spawned()
    {
        hasSpawned = true;
        collectInProgress = false;
        ApplyCollectedVisualState(IsCollected);
    }

    public override void Render()
    {
        if (!hasSpawned)
        {
            return;
        }

        bool collected = IsCollected;
        if (!visualStateInitialized || collected != lastVisualCollected)
        {
            ApplyCollectedVisualState(collected);
        }
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

    private void OnTriggerStay(Collider other)
    {
        TryCollect(other);
    }

    private void TryCollect(Collider other)
    {
        if (!hasSpawned || collectInProgress)
        {
            return;
        }

        if (IsCollected)
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
            ApplyCollect(collector, collectorRef);
        }
        else
        {
            RPC_RequestCollect(collectorRef);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestCollect(PlayerRef collectorRef)
    {
        if (!hasSpawned || collectInProgress || IsCollected)
        {
            return;
        }

        SharedModePlayerController collector = ResolveCollector(collectorRef);
        if (collector == null || collector.IsDead)
        {
            return;
        }

        ApplyCollect(collector, collectorRef);
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

    private void ApplyCollect(SharedModePlayerController collector, PlayerRef collectorRef)
    {
        if (IsCollected)
        {
            return;
        }

        collectInProgress = true;
        IsCollected = true;
        ApplyCollectedVisualState(true);

        if (healCollectorToFull)
        {
            collector.RPC_RequestFullHeal();
        }

        collector.RPC_RequestAddCollectedItem(1);

        if (logCollect)
        {
            Debug.Log($"{name}: collected by {collectorRef}.");
        }

        if (destroyOnCollect && Runner != null && Object != null && Object.IsValid)
        {
            Runner.Despawn(Object);
        }
    }

    private void ApplyCollectedVisualState(bool collected)
    {
        if (itemCollider != null)
        {
            itemCollider.enabled = !collected;
        }

        if (itemRenderers != null)
        {
            for (int i = 0; i < itemRenderers.Length; i++)
            {
                if (itemRenderers[i] != null)
                {
                    itemRenderers[i].enabled = !collected;
                }
            }
        }

        lastVisualCollected = collected;
        visualStateInitialized = true;
    }
}
