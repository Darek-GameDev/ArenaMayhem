using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SpikeTrap : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damage = 3;
    [SerializeField] private bool logHits = false;

    [Header("Timing")]
    [SerializeField] private float hiddenDuration = 1.5f;
    [SerializeField] private float activeDuration = 0.5f;
    [SerializeField] private float startDelay = 0f;
    [SerializeField] private float moveDuration = 0.15f;
    [SerializeField] private bool startActive = false;

    [Header("Random Timing")]
    [SerializeField] private bool useRandomTiming = true;
    [SerializeField] private bool randomizeStartupOffset = true;
    [SerializeField] private Vector2 hiddenDurationRange = new Vector2(1.0f, 2.0f);
    [SerializeField] private Vector2 activeDurationRange = new Vector2(0.35f, 0.8f);

    [Header("References")]
    [SerializeField] private Collider damageCollider;
    [SerializeField] private Transform spikeVisual;

    [Header("Spike Local Positions")]
    [SerializeField] private Vector3 hiddenLocalPosition = new Vector3(0f, -0.5f, 0f);
    [SerializeField] private Vector3 activeLocalPosition = new Vector3(0f, 0f, 0f);

    private readonly HashSet<PlayerHealth> hitTargets = new HashSet<PlayerHealth>();
    private Coroutine cycleRoutine;
    private bool isRaised;
    private float hiddenMin;
    private float hiddenMax;
    private float activeMin;
    private float activeMax;

    private void Awake()
    {
        if (damageCollider == null)
        {
            damageCollider = GetComponent<Collider>();
        }

        if (spikeVisual == null)
        {
            spikeVisual = transform;
        }

        hiddenDuration = Mathf.Max(0f, hiddenDuration);
        activeDuration = Mathf.Max(0.05f, activeDuration);
        moveDuration = Mathf.Max(0.01f, moveDuration);
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

        isRaised = false;
        SetDamageActive(false);

        if (spikeVisual != null)
        {
            spikeVisual.localPosition = hiddenLocalPosition;
        }
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
            if (!isRaised)
            {
                yield return new WaitForSeconds(GetHiddenDuration());
                yield return MoveSpike(hiddenLocalPosition, activeLocalPosition);
                SetDamageActive(true);
                hitTargets.Clear();
                isRaised = true;
            }
            else
            {
                yield return new WaitForSeconds(GetActiveDuration());
                SetDamageActive(false);
                yield return MoveSpike(activeLocalPosition, hiddenLocalPosition);
                isRaised = false;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryDamage(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryDamage(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision != null)
        {
            TryDamage(collision.collider);
        }
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
        if (!isRaised || other == null)
        {
            return;
        }

        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null || playerHealth.IsDead)
        {
            return;
        }

        if (hitTargets.Contains(playerHealth))
        {
            return;
        }

        hitTargets.Add(playerHealth);
        playerHealth.TakeEnvironmentalDamage(damage);

        if (logHits)
        {
            Debug.Log($"{name} hit {playerHealth.name} for {damage}.");
        }
    }

    private void InitializeState()
    {
        if (spikeVisual == null)
        {
            return;
        }

        isRaised = startActive;
        spikeVisual.localPosition = isRaised ? activeLocalPosition : hiddenLocalPosition;
        SetDamageActive(isRaised);

        if (isRaised)
        {
            hitTargets.Clear();
        }
    }

    private IEnumerator MoveSpike(Vector3 from, Vector3 to)
    {
        if (spikeVisual == null)
        {
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            float eased = t * t * (3f - 2f * t);
            spikeVisual.localPosition = Vector3.LerpUnclamped(from, to, eased);
            yield return null;
        }

        spikeVisual.localPosition = to;
    }

    private void SetDamageActive(bool active)
    {
        if (damageCollider != null)
        {
            damageCollider.enabled = active;
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