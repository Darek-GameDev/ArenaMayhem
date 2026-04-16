using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class FireJet : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damagePerTick = 1;
    [SerializeField] private float damageTick = 0.2f;
    [SerializeField] private bool logHits = false;

    [Header("Timing")]
    [SerializeField] private float hiddenDuration = 2.0f;
    [SerializeField] private float activeDuration = 1.5f;
    [SerializeField] private float startDelay = 0f;
    [SerializeField] private bool startActive = false;

    [Header("Random Timing")]
    [SerializeField] private bool useRandomTiming = true;
    [SerializeField] private bool randomizeStartupOffset = true;
    [SerializeField] private Vector2 hiddenDurationRange = new Vector2(1.2f, 2.8f);
    [SerializeField] private Vector2 activeDurationRange = new Vector2(0.8f, 1.8f);

    [Header("References")]
    [SerializeField] private Collider jetCollider;
    [SerializeField] private Transform jetVisual;

    private readonly HashSet<PlayerHealth> hitTargets = new HashSet<PlayerHealth>();
    private readonly Dictionary<PlayerHealth, float> lastDamageTime = new Dictionary<PlayerHealth, float>();
    private Coroutine cycleRoutine;
    private bool isActive;
    private float hiddenMin;
    private float hiddenMax;
    private float activeMin;
    private float activeMax;

    private void Awake()
    {
        if (jetCollider == null)
        {
            jetCollider = GetComponent<Collider>();
        }

        if (jetVisual == null)
        {
            jetVisual = transform;
        }

        hiddenDuration = Mathf.Max(0f, hiddenDuration);
        activeDuration = Mathf.Max(0.05f, activeDuration);
        damageTick = Mathf.Max(0.01f, damageTick);

        hiddenMin = Mathf.Max(0f, Mathf.Min(hiddenDurationRange.x, hiddenDurationRange.y));
        hiddenMax = Mathf.Max(hiddenMin, Mathf.Max(hiddenDurationRange.x, hiddenDurationRange.y));
        activeMin = Mathf.Max(0.05f, Mathf.Min(activeDurationRange.x, activeDurationRange.y));
        activeMax = Mathf.Max(activeMin, Mathf.Max(activeDurationRange.x, activeDurationRange.y));

        InitializeState();
    }

    private void OnEnable()
    {
        cycleRoutine = StartCoroutine(CycleRoutine());
    }

    private void OnDisable()
    {
        if (cycleRoutine != null)
        {
            StopCoroutine(cycleRoutine);
            cycleRoutine = null;
        }

        isActive = false;
        SetJetActive(false);
    }

    private IEnumerator CycleRoutine()
    {
        float startupWait = Mathf.Max(0f, startDelay);
        if (useRandomTiming && randomizeStartupOffset)
        {
            startupWait += Random.Range(0f, hiddenMax + activeMax);
        }

        if (startupWait > 0f)
        {
            yield return new WaitForSeconds(startupWait);
        }

        while (true)
        {
            if (!isActive)
            {
                yield return new WaitForSeconds(GetHiddenDuration());
                SetJetActive(true);
                hitTargets.Clear();
                lastDamageTime.Clear();
                isActive = true;
            }
            else
            {
                yield return new WaitForSeconds(GetActiveDuration());
                SetJetActive(false);
                lastDamageTime.Clear();
                isActive = false;
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        TryDamage(other);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision != null)
        {
            TryDamage(collision.collider);
        }
    }

    private void TryDamage(Collider other)
    {
        if (!isActive || other == null)
        {
            return;
        }

        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null || playerHealth.IsDead)
        {
            return;
        }

        if (!hitTargets.Contains(playerHealth))
        {
            hitTargets.Add(playerHealth);
        }

        float now = Time.time;
        float lastDamage = 0f;
        lastDamageTime.TryGetValue(playerHealth, out lastDamage);

        if (now - lastDamage >= damageTick)
        {
            playerHealth.TakeEnvironmentalDamage(damagePerTick);
            lastDamageTime[playerHealth] = now;

            if (logHits)
            {
                Debug.Log($"{name} hit {playerHealth.name} for {damagePerTick}.");
            }
        }
    }

    private void InitializeState()
    {
        isActive = startActive;
        SetJetActive(isActive);
    }

    private void SetJetActive(bool active)
    {
        if (jetCollider != null)
        {
            jetCollider.enabled = active;
        }

        if (jetVisual != null)
        {
            jetVisual.gameObject.SetActive(active);
        }
    }

    private float GetHiddenDuration()
    {
        if (!useRandomTiming)
        {
            return hiddenDuration;
        }

        return Random.Range(hiddenMin, hiddenMax);
    }

    private float GetActiveDuration()
    {
        if (!useRandomTiming)
        {
            return activeDuration;
        }

        return Random.Range(activeMin, activeMax);
    }
}
