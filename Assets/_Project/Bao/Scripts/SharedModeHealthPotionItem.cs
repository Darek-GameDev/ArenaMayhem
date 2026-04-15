using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class SharedModeHealthPotionItem : NetworkBehaviour
{
    public enum ItemEffectType
    {
        Heal = 0,
        Speed = 1,
    }

    [SerializeField] private ItemEffectType itemEffect = ItemEffectType.Heal;
    [SerializeField] private bool healCollectorToFull = true;
    [SerializeField] private bool destroyOnCollect = true;
    [SerializeField] private bool logCollect = false;
    [SerializeField] private float speedBoostMultiplier = 1.5f;
    [SerializeField] private float speedBoostDuration = 5f;
    [SerializeField] private Renderer[] itemRenderers;

    [Networked] private NetworkBool IsCollected { get; set; }

    private static readonly FieldInfo RunSpeedField = typeof(SharedModePlayerController).GetField("runSpeed", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly Dictionary<SharedModePlayerController, SpeedBoostState> SpeedBoostStates = new Dictionary<SharedModePlayerController, SpeedBoostState>();

    private bool hasSpawned;
    private bool collectInProgress;
    private bool visualStateInitialized;
    private bool lastVisualCollected;
    private Collider itemCollider;

    private sealed class SpeedBoostState
    {
        public float OriginalRunSpeed;
        public int Token;
    }

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

        if (itemEffect == ItemEffectType.Heal)
        {
            if (healCollectorToFull)
            {
                collector.RPC_RequestFullHeal();
            }
        }
        else if (itemEffect == ItemEffectType.Speed)
        {
            ApplySpeedBoost(collector);
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

    private void ApplySpeedBoost(SharedModePlayerController collector)
    {
        if (collector == null || collector.Object == null || !collector.Object.IsValid)
        {
            return;
        }

        if (RunSpeedField == null)
        {
            Debug.LogWarning($"{name}: cannot apply speed boost because runSpeed field was not found.");
            return;
        }

        SpeedBoostState state = GetOrCreateSpeedBoostState(collector);
        state.Token++;

        float baseRunSpeed = state.OriginalRunSpeed;
        float boostedRunSpeed = Mathf.Max(0f, baseRunSpeed) * Mathf.Max(1f, speedBoostMultiplier);

        SetRunSpeed(collector, boostedRunSpeed);

        if (speedBoostDuration > 0f)
        {
            collector.StartCoroutine(RestoreSpeedAfterDelay(collector, state.Token, speedBoostDuration));
        }
    }

    private SpeedBoostState GetOrCreateSpeedBoostState(SharedModePlayerController collector)
    {
        if (!SpeedBoostStates.TryGetValue(collector, out SpeedBoostState state) || state == null)
        {
            state = new SpeedBoostState
            {
                OriginalRunSpeed = GetRunSpeed(collector),
                Token = 0,
            };

            SpeedBoostStates[collector] = state;
        }

        return state;
    }

    private IEnumerator RestoreSpeedAfterDelay(SharedModePlayerController collector, int token, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (collector == null || collector.Object == null || !collector.Object.IsValid)
        {
            SpeedBoostStates.Remove(collector);
            yield break;
        }

        if (!SpeedBoostStates.TryGetValue(collector, out SpeedBoostState state) || state == null || state.Token != token)
        {
            yield break;
        }

        SetRunSpeed(collector, state.OriginalRunSpeed);
        SpeedBoostStates.Remove(collector);
    }

    private float GetRunSpeed(SharedModePlayerController collector)
    {
        if (RunSpeedField == null || collector == null)
        {
            return 0f;
        }

        object value = RunSpeedField.GetValue(collector);
        return value is float speed ? speed : 0f;
    }

    private void SetRunSpeed(SharedModePlayerController collector, float speed)
    {
        if (RunSpeedField == null || collector == null)
        {
            return;
        }

        RunSpeedField.SetValue(collector, speed);
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
