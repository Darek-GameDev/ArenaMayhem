using Fusion;
using System.Collections;
using System.Reflection;
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
        SpeedPotionBuffHandler buffHandler = collector.GetComponent<SpeedPotionBuffHandler>();
        if (buffHandler == null)
        {
            buffHandler = collector.gameObject.AddComponent<SpeedPotionBuffHandler>();
        }

        buffHandler.ApplyBuff(collector, speedAmount, buffDurationSeconds);

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

public class SpeedPotionBuffHandler : MonoBehaviour
{
    private static readonly FieldInfo RunSpeedField =
        typeof(SharedModePlayerController).GetField("runSpeed", BindingFlags.Instance | BindingFlags.NonPublic);

    private Coroutine activeRoutine;
    private SharedModePlayerController target;
    private float activeBuffAmount;

    public void ApplyBuff(SharedModePlayerController controller, float speedAmount, float durationSeconds)
    {
        if (controller == null || speedAmount <= 0f || durationSeconds <= 0f || RunSpeedField == null)
        {
            return;
        }

        target = controller;

        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            RevertCurrentBuff();
        }

        float currentRunSpeed = (float)RunSpeedField.GetValue(target);
        RunSpeedField.SetValue(target, currentRunSpeed + speedAmount);
        activeBuffAmount = speedAmount;
        activeRoutine = StartCoroutine(RevertAfterDelay(durationSeconds));
    }

    private IEnumerator RevertAfterDelay(float durationSeconds)
    {
        yield return new WaitForSeconds(durationSeconds);
        RevertCurrentBuff();
        activeRoutine = null;
    }

    private void RevertCurrentBuff()
    {
        if (target == null || RunSpeedField == null || activeBuffAmount <= 0f)
        {
            activeBuffAmount = 0f;
            return;
        }

        float currentRunSpeed = (float)RunSpeedField.GetValue(target);
        RunSpeedField.SetValue(target, Mathf.Max(0f, currentRunSpeed - activeBuffAmount));
        activeBuffAmount = 0f;
    }

    private void OnDisable()
    {
        RevertCurrentBuff();
    }

    private void OnDestroy()
    {
        RevertCurrentBuff();
    }
}
