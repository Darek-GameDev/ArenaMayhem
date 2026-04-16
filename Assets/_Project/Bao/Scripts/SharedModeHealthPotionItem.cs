using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Fusion;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class SharedModeHealthPotionItem : NetworkBehaviour
{
    public enum ItemEffectType
    {
        Heal = 0,
        Speed = 1,
        Damage = 2,
        Invisibility = 3,
    }

    [SerializeField] private ItemEffectType itemEffect = ItemEffectType.Heal;
    [SerializeField] private bool healCollectorToFull = true;
    [SerializeField] private bool destroyOnCollect = true;
    [SerializeField] private bool logCollect = false;
    [SerializeField] private float speedBoostMultiplier = 1.5f;
    [SerializeField] private float speedBoostDuration = 5f;
    [SerializeField] private float damageBoostMultiplier = 1.5f;
    [SerializeField] private float damageBoostDuration = 5f;
    [SerializeField] private float invisibilityDuration = 5f;
    [SerializeField] private float localPlayerInvisibilityAlpha = 0.3f;
    [SerializeField] private Renderer[] itemRenderers;

    [Networked] private NetworkBool IsCollected { get; set; }
    [Networked] private PlayerRef CollectedBy { get; set; }
    [Networked] private float InvisibilityEndTime { get; set; }

    private static readonly FieldInfo RunSpeedField = typeof(SharedModePlayerController).GetField("runSpeed", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly Dictionary<SharedModePlayerController, SpeedBoostState> SpeedBoostStates = new Dictionary<SharedModePlayerController, SpeedBoostState>();
    private static readonly FieldInfo WeaponDamageField = typeof(Weapon).GetField("damage", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo BowDamageField = typeof(BowWeapon).GetField("damage", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo BowSecondaryDamageField = typeof(BowWeapon).GetField("secondaryDamage", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly Dictionary<SharedModePlayerController, DamageBoostState> DamageBoostStates = new Dictionary<SharedModePlayerController, DamageBoostState>();
    private static readonly Dictionary<PlayerRef, InvisibilityState> InvisibilityStates = new Dictionary<PlayerRef, InvisibilityState>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        SpeedBoostStates.Clear();
        DamageBoostStates.Clear();
        InvisibilityStates.Clear();
    }

    private bool hasSpawned;
    private bool collectInProgress;
    private bool visualStateInitialized;
    private bool lastVisualCollected;
    private bool invisibilityVisualApplied;
    private PlayerRef lastInvisibilityCollectorRef;
    private Coroutine delayedDespawnRoutine;
    private Collider itemCollider;

    private sealed class SpeedBoostState
    {
        public float OriginalRunSpeed;
        public int Token;
    }

    private sealed class DamageBoostState
    {
        public int Token;
        public readonly List<DamageFieldState> FieldStates = new List<DamageFieldState>();
    }

    private sealed class DamageFieldState
    {
        public Component TargetComponent;
        public FieldInfo Field;
        public int OriginalValue;
    }

    private sealed class InvisibilityState
    {
        public int Token;
        public float EndTime;
        public readonly List<RendererState> RendererStates = new List<RendererState>();
        public readonly List<HealthBarState> HealthBarStates = new List<HealthBarState>();
        public readonly List<CanvasState> RuntimeCanvasStates = new List<CanvasState>();
    }

    private sealed class RendererState
    {
        public Renderer Renderer;
        public bool OriginalEnabled;
        public ShadowCastingMode OriginalShadowCastingMode;
        public bool OriginalReceiveShadows;
        public List<Color> OriginalMaterialColors = new List<Color>();
        public List<int> OriginalRenderQueues = new List<int>();
    }

    private sealed class HealthBarState
    {
        public WorldSpaceHealthBar HealthBar;
        public bool OriginalEnabled;
        public Canvas Canvas;
        public bool OriginalCanvasEnabled;
    }

    private sealed class CanvasState
    {
        public Canvas Canvas;
        public bool OriginalEnabled;
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
        invisibilityVisualApplied = false;
        lastInvisibilityCollectorRef = default;

        if (HasStateAuthority)
        {
            IsCollected = false;
            CollectedBy = default;
            InvisibilityEndTime = 0f;
        }

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

        if (itemEffect == ItemEffectType.Invisibility)
        {
            if (IsCollected && !invisibilityVisualApplied)
            {
                invisibilityVisualApplied = true;
            }
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        hasSpawned = false;
        delayedDespawnRoutine = null;
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

        if (!HasStateAuthority && !collector.Object.HasInputAuthority)
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
            SharedModePlayerController[] controllers = FindObjectsByType<SharedModePlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < controllers.Length; i++)
            {
                SharedModePlayerController candidate = controllers[i];
                if (candidate != null && candidate.Object != null && candidate.Object.IsValid && candidate.Object.InputAuthority == collectorRef)
                {
                    return candidate;
                }
            }

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
        CollectedBy = collectorRef;
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
        else if (itemEffect == ItemEffectType.Damage)
        {
            ApplyDamageBoost(collector);
        }
        else if (itemEffect == ItemEffectType.Invisibility)
        {
            ApplyInvisibility(collector, collectorRef);
        }

        collector.RPC_RequestAddCollectedItem(1);

        if (logCollect)
        {
            Debug.Log($"{name}: collected by {collectorRef}.");
        }

        if (destroyOnCollect && Runner != null && Object != null && Object.IsValid)
        {
            if (itemEffect == ItemEffectType.Invisibility && invisibilityDuration > 0f)
            {
                if (delayedDespawnRoutine != null)
                {
                    StopCoroutine(delayedDespawnRoutine);
                }

                delayedDespawnRoutine = StartCoroutine(DespawnAfterDelay(invisibilityDuration + 0.1f));
            }
            else
            {
                Runner.Despawn(Object);
            }
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

    private void ApplyDamageBoost(SharedModePlayerController collector)
    {
        if (collector == null || collector.Object == null || !collector.Object.IsValid)
        {
            return;
        }

        if (WeaponDamageField == null && BowDamageField == null && BowSecondaryDamageField == null)
        {
            Debug.LogWarning($"{name}: cannot apply damage boost because no damage fields were found.");
            return;
        }

        DamageBoostState state = GetOrCreateDamageBoostState(collector);
        state.Token++;

        float multiplier = Mathf.Max(1f, damageBoostMultiplier);
        for (int i = 0; i < state.FieldStates.Count; i++)
        {
            DamageFieldState fieldState = state.FieldStates[i];
            if (fieldState == null || fieldState.TargetComponent == null || fieldState.Field == null)
            {
                continue;
            }

            int boostedDamage = Mathf.Max(1, Mathf.RoundToInt(fieldState.OriginalValue * multiplier));
            fieldState.Field.SetValue(fieldState.TargetComponent, boostedDamage);
        }

        if (damageBoostDuration > 0f)
        {
            collector.StartCoroutine(RestoreDamageAfterDelay(collector, state.Token, damageBoostDuration));
        }
    }

    private void ApplyInvisibility(SharedModePlayerController collector, PlayerRef collectorRef)
    {
        if (collector == null || collector.Object == null || !collector.Object.IsValid || !collectorRef.IsRealPlayer)
        {
            return;
        }

        InvisibilityState state = GetOrCreateInvisibilityState(collectorRef);
        state.Token++;

        float endTime = Runner != null ? Runner.SimulationTime + invisibilityDuration : Time.time + invisibilityDuration;
        state.EndTime = endTime;
        InvisibilityEndTime = endTime;

        CollectedBy = collectorRef;
        ApplyCollectorInvisibilityLocal(collectorRef, true);
        RPC_SetCollectorInvisibility(collectorRef, true, state.Token, endTime);
        invisibilityVisualApplied = true;
        lastInvisibilityCollectorRef = collectorRef;

        if (invisibilityDuration > 0f)
        {
            collector.StartCoroutine(RestoreInvisibilityAtSimulationTime(collectorRef, state.Token, endTime));
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

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SetCollectorInvisibility(PlayerRef collectorRef, bool invisible, int token, float endTime)
    {
        if (HasStateAuthority)
        {
            return;
        }

        if (invisible)
        {
            InvisibilityState state = GetOrCreateInvisibilityState(collectorRef);
            state.Token = token;
            state.EndTime = endTime;

            ApplyCollectorInvisibilityLocal(collectorRef, true);

            SharedModePlayerController collector = ResolveCollector(collectorRef);
            if (collector != null && invisibilityDuration > 0f)
            {
                collector.StartCoroutine(RestoreInvisibilityAtSimulationTime(collectorRef, token, endTime));
            }

            return;
        }

        ApplyCollectorInvisibilityLocal(collectorRef, invisible);
    }

    private DamageBoostState GetOrCreateDamageBoostState(SharedModePlayerController collector)
    {
        if (!DamageBoostStates.TryGetValue(collector, out DamageBoostState state) || state == null)
        {
            state = new DamageBoostState
            {
                Token = 0,
            };

            CacheDamageFieldStates(collector, state);
            DamageBoostStates[collector] = state;
        }
        else if (state.FieldStates.Count == 0)
        {
            CacheDamageFieldStates(collector, state);
        }

        return state;
    }

    private InvisibilityState GetOrCreateInvisibilityState(PlayerRef collectorRef)
    {
        if (!InvisibilityStates.TryGetValue(collectorRef, out InvisibilityState state) || state == null)
        {
            state = new InvisibilityState
            {
                Token = 0,
            };

            InvisibilityStates[collectorRef] = state;
        }

        return state;
    }

    private void CacheDamageFieldStates(SharedModePlayerController collector, DamageBoostState state)
    {
        if (collector == null || state == null)
        {
            return;
        }

        Weapon[] weapons = collector.GetComponentsInChildren<Weapon>(true);
        for (int i = 0; i < weapons.Length; i++)
        {
            CacheDamageFieldState(weapons[i], WeaponDamageField, state);
        }

        BowWeapon[] bowWeapons = collector.GetComponentsInChildren<BowWeapon>(true);
        for (int i = 0; i < bowWeapons.Length; i++)
        {
            CacheDamageFieldState(bowWeapons[i], BowDamageField, state);
            CacheDamageFieldState(bowWeapons[i], BowSecondaryDamageField, state);
        }
    }

    private void CacheDamageFieldState(Component component, FieldInfo field, DamageBoostState state)
    {
        if (component == null || field == null || state == null)
        {
            return;
        }

        object value = field.GetValue(component);
        if (value is not int originalValue)
        {
            return;
        }

        state.FieldStates.Add(new DamageFieldState
        {
            TargetComponent = component,
            Field = field,
            OriginalValue = originalValue,
        });
    }

    private void ApplyCollectorInvisibilityLocal(PlayerRef collectorRef, bool invisible)
    {
        SharedModePlayerController collector = ResolveCollector(collectorRef);
        if (collector == null || collector.Object == null || !collector.Object.IsValid)
        {
            return;
        }

        bool isLocalPlayer = Runner != null && collectorRef == Runner.LocalPlayer;

        if (invisible)
        {
            InvisibilityState state = GetOrCreateInvisibilityState(collectorRef);
            if (state.RendererStates.Count == 0)
            {
                CacheInvisibilityRenderers(collector, state);
            }

            CacheHealthBarState(collector, state);

            if (isLocalPlayer)
            {
                ApplyInvisibilityAlpha(state, localPlayerInvisibilityAlpha);
            }
            else
            {
                ApplyInvisibilityDisabled(state);
            }

            ApplyHealthBarVisible(state, false);

            return;
        }

        if (!InvisibilityStates.TryGetValue(collectorRef, out InvisibilityState restoreState) || restoreState == null)
        {
            return;
        }

        // Restore all renderers to original state
        for (int i = 0; i < restoreState.RendererStates.Count; i++)
        {
            RendererState rendererState = restoreState.RendererStates[i];
            if (rendererState?.Renderer != null)
            {
                RestoreRendererAlpha(rendererState);
                rendererState.Renderer.enabled = rendererState.OriginalEnabled;
            }
        }

        ApplyHealthBarVisible(restoreState, true);
    }

    private void ApplyInvisibilityAlpha(InvisibilityState state, float alpha)
    {
        if (state == null)
        {
            return;
        }

        for (int i = 0; i < state.RendererStates.Count; i++)
        {
            RendererState rendererState = state.RendererStates[i];
            if (rendererState?.Renderer == null)
            {
                continue;
            }

            rendererState.Renderer.enabled = true;
            rendererState.Renderer.shadowCastingMode = ShadowCastingMode.Off;
            rendererState.Renderer.receiveShadows = false;

            Renderer renderer = rendererState.Renderer;
            Material[] materials = renderer.materials;
            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material mat = materials[materialIndex];
                if (mat == null)
                {
                    continue;
                }

                Color color = mat.color;
                color.a = alpha;
                mat.color = color;

                if (mat.HasProperty("_BaseColor"))
                {
                    Color baseColor = mat.GetColor("_BaseColor");
                    baseColor.a = alpha;
                    mat.SetColor("_BaseColor", baseColor);
                }

                if (mat.HasProperty("_Color"))
                {
                    Color legacyColor = mat.GetColor("_Color");
                    legacyColor.a = alpha;
                    mat.SetColor("_Color", legacyColor);
                }

                if (mat.HasProperty("_Surface"))
                {
                    mat.SetFloat("_Surface", 1f);
                }

                if (mat.HasProperty("_Mode"))
                {
                    mat.SetFloat("_Mode", 3f);
                }

                if (mat.HasProperty("_SrcBlend"))
                {
                    mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                }

                if (mat.HasProperty("_DstBlend"))
                {
                    mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                }

                if (mat.HasProperty("_ZWrite"))
                {
                    mat.SetFloat("_ZWrite", 0f);
                }

                mat.SetOverrideTag("RenderType", "Transparent");
                mat.renderQueue = (int)RenderQueue.Transparent;
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.DisableKeyword("_SURFACE_TYPE_OPAQUE");
            }
        }
    }

    private void ApplyInvisibilityDisabled(InvisibilityState state)
    {
        if (state == null)
        {
            return;
        }

        for (int i = 0; i < state.RendererStates.Count; i++)
        {
            RendererState rendererState = state.RendererStates[i];
            if (rendererState?.Renderer != null)
            {
                rendererState.Renderer.enabled = false;
            }
        }
    }

    private void RestoreRendererAlpha(RendererState rendererState)
    {
        if (rendererState?.Renderer == null)
        {
            return;
        }

        Renderer renderer = rendererState.Renderer;
        renderer.shadowCastingMode = rendererState.OriginalShadowCastingMode;
        renderer.receiveShadows = rendererState.OriginalReceiveShadows;

        Material[] materials = renderer.materials;
        for (int i = 0; i < materials.Length; i++)
        {
            Material mat = materials[i];
            if (mat == null)
            {
                continue;
            }

            if (i < rendererState.OriginalMaterialColors.Count)
            {
                mat.color = rendererState.OriginalMaterialColors[i];
            }

            if (mat.HasProperty("_BaseColor") && i < rendererState.OriginalMaterialColors.Count)
            {
                mat.SetColor("_BaseColor", rendererState.OriginalMaterialColors[i]);
            }

            if (mat.HasProperty("_Color") && i < rendererState.OriginalMaterialColors.Count)
            {
                mat.SetColor("_Color", rendererState.OriginalMaterialColors[i]);
            }

            if (i < rendererState.OriginalRenderQueues.Count)
            {
                mat.renderQueue = rendererState.OriginalRenderQueues[i];
            }

            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 0f);
            }

            if (mat.HasProperty("_Mode"))
            {
                mat.SetFloat("_Mode", 0f);
            }

            if (mat.HasProperty("_SrcBlend"))
            {
                mat.SetFloat("_SrcBlend", (float)BlendMode.One);
            }

            if (mat.HasProperty("_DstBlend"))
            {
                mat.SetFloat("_DstBlend", (float)BlendMode.Zero);
            }

            if (mat.HasProperty("_ZWrite"))
            {
                mat.SetFloat("_ZWrite", 1f);
            }

            mat.SetOverrideTag("RenderType", string.Empty);
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_SURFACE_TYPE_OPAQUE");
        }
    }

    private void CacheInvisibilityRenderers(SharedModePlayerController collector, InvisibilityState state)
    {
        if (collector == null || state == null)
        {
            return;
        }

        Transform searchRoot = collector.transform != null ? collector.transform.root : collector.transform;
        Renderer[] renderers = searchRoot != null
            ? searchRoot.GetComponentsInChildren<Renderer>(true)
            : collector.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            RendererState rendererState = new RendererState
            {
                Renderer = renderer,
                OriginalEnabled = renderer.enabled,
                OriginalShadowCastingMode = renderer.shadowCastingMode,
                OriginalReceiveShadows = renderer.receiveShadows,
            };

            // Cache original material colors
            Material[] materials = renderer.materials;
            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material mat = materials[materialIndex];
                if (mat != null)
                {
                    rendererState.OriginalMaterialColors.Add(mat.color);
                    rendererState.OriginalRenderQueues.Add(mat.renderQueue);
                }
            }

            state.RendererStates.Add(rendererState);
        }
    }

    private void CacheHealthBarState(SharedModePlayerController collector, InvisibilityState state)
    {
        if (collector == null || state == null)
        {
            return;
        }

        WorldSpaceHealthBar[] healthBars = collector.GetComponentsInChildren<WorldSpaceHealthBar>(true);
        for (int i = 0; i < healthBars.Length; i++)
        {
            WorldSpaceHealthBar healthBar = healthBars[i];
            if (healthBar == null)
            {
                continue;
            }

            bool alreadyCached = false;
            for (int j = 0; j < state.HealthBarStates.Count; j++)
            {
                if (state.HealthBarStates[j] != null && state.HealthBarStates[j].HealthBar == healthBar)
                {
                    alreadyCached = true;
                    break;
                }
            }

            if (alreadyCached)
            {
                continue;
            }

            state.HealthBarStates.Add(new HealthBarState
            {
                HealthBar = healthBar,
                OriginalEnabled = healthBar.enabled,
                Canvas = healthBar.GetComponentInChildren<Canvas>(true),
                OriginalCanvasEnabled = healthBar.GetComponentInChildren<Canvas>(true) != null && healthBar.GetComponentInChildren<Canvas>(true).enabled,
            });
        }

        Transform root = collector.transform != null ? collector.transform.root : collector.transform;
        if (root == null)
        {
            return;
        }

        Canvas[] canvases = root.GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null)
            {
                continue;
            }

            string canvasName = canvas.gameObject.name;
            if (canvasName != "WorldHealthUI" && canvasName != "HealthBar")
            {
                continue;
            }

            bool alreadyCachedCanvas = false;
            for (int j = 0; j < state.RuntimeCanvasStates.Count; j++)
            {
                if (state.RuntimeCanvasStates[j] != null && state.RuntimeCanvasStates[j].Canvas == canvas)
                {
                    alreadyCachedCanvas = true;
                    break;
                }
            }

            if (alreadyCachedCanvas)
            {
                continue;
            }

            state.RuntimeCanvasStates.Add(new CanvasState
            {
                Canvas = canvas,
                OriginalEnabled = canvas.enabled,
            });
        }
    }

    private void ApplyHealthBarVisible(InvisibilityState state, bool visible)
    {
        if (state == null)
        {
            return;
        }

        for (int i = 0; i < state.HealthBarStates.Count; i++)
        {
            HealthBarState healthBarState = state.HealthBarStates[i];
            if (healthBarState?.HealthBar == null)
            {
                continue;
            }

            if (healthBarState.Canvas != null)
            {
                healthBarState.Canvas.enabled = visible ? healthBarState.OriginalCanvasEnabled : false;
            }

            healthBarState.HealthBar.enabled = visible ? healthBarState.OriginalEnabled : false;
        }

        for (int i = 0; i < state.RuntimeCanvasStates.Count; i++)
        {
            CanvasState canvasState = state.RuntimeCanvasStates[i];
            if (canvasState?.Canvas == null)
            {
                continue;
            }

            canvasState.Canvas.enabled = visible ? canvasState.OriginalEnabled : false;
        }
    }

    private IEnumerator DespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        delayedDespawnRoutine = null;

        if (!HasStateAuthority || Runner == null || Object == null || !Object.IsValid)
        {
            yield break;
        }

        Runner.Despawn(Object);
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

    private IEnumerator RestoreDamageAfterDelay(SharedModePlayerController collector, int token, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (collector == null || collector.Object == null || !collector.Object.IsValid)
        {
            DamageBoostStates.Remove(collector);
            yield break;
        }

        if (!DamageBoostStates.TryGetValue(collector, out DamageBoostState state) || state == null || state.Token != token)
        {
            yield break;
        }

        for (int i = 0; i < state.FieldStates.Count; i++)
        {
            DamageFieldState fieldState = state.FieldStates[i];
            if (fieldState == null || fieldState.TargetComponent == null || fieldState.Field == null)
            {
                continue;
            }

            fieldState.Field.SetValue(fieldState.TargetComponent, fieldState.OriginalValue);
        }

        DamageBoostStates.Remove(collector);
    }

    private IEnumerator RestoreInvisibilityAtSimulationTime(PlayerRef collectorRef, int token, float endTime)
    {
        while (Runner != null && Runner.SimulationTime < endTime)
        {
            yield return null;
        }

        if (!InvisibilityStates.TryGetValue(collectorRef, out InvisibilityState state) || state == null || state.Token != token)
        {
            yield break;
        }

        if (Mathf.Abs(state.EndTime - endTime) > 0.001f)
        {
            yield break;
        }

        ApplyCollectorInvisibilityLocal(collectorRef, false);
        InvisibilityStates.Remove(collectorRef);

        invisibilityVisualApplied = false;
        lastInvisibilityCollectorRef = default;
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
