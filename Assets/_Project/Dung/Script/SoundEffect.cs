using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class SoundEffect : MonoBehaviour
{
    private enum CharacterType
    {
        Melee,
        Bow,
    }

    [Header("General")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private bool autoDetectCharacterType = true;
    [SerializeField] private CharacterType characterType = CharacterType.Melee;
    [SerializeField] [Range(0f, 1f)] private float volumeMultiplier = 1f;
    [SerializeField] [Range(0f, 0.5f)] private float randomPitchRange = 0.05f;
    [SerializeField] private bool debugLogs;
    [SerializeField] private bool useMouseInput = false;
    [SerializeField] private bool useDedicatedRuntimeSource = true;
    [SerializeField] private bool useCameraFallbackPlayAtPoint = true;

    [Header("Melee Combo")]
    [SerializeField] private float meleeComboResetSeconds = 0.8f;

    [Header("Audible Safety")]
    [SerializeField] private bool forceAudibleAtRuntime = true;
    [SerializeField] [Range(0f, 1f)] private float fallbackSourceVolume = 1f;

    [Header("Melee")]
    [SerializeField] private AudioClip meleeAttack1;
    [SerializeField] private AudioClip meleeAttack2;
    [SerializeField] private AudioClip meleeAttack3;
    [SerializeField] private AudioClip meleeBlock;

    [Header("Health")]
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip deathSound;

    [Header("Movement")]
    [SerializeField] private AudioClip jumpUpSound;
    [SerializeField] private AudioClip landSound;
    [SerializeField] private bool autoPlayJumpLandByGroundedState = true;
    [SerializeField] private bool playJumpSoundWhenLeaveGround = true;
    [SerializeField] private bool playLandSoundWhenTouchGround = true;
    [SerializeField] private float minAirborneTimeForLandSound = 0.08f;

    [Header("Bow")]
    [SerializeField] private AudioClip bowDraw;
    [SerializeField] private AudioClip bowShoot;

    private PlayerAttack playerAttack;
    private PlayerHealth playerHealth;
    private SharedModePlayerController sharedModeController;
    private CharacterController characterController;
    private Fusion.NetworkCharacterController networkCharacterController;
    private int meleeComboStep;
    private float lastMeleeClickTime = -999f;
    private bool wasAimHeld;
    private AudioSource dedicatedRuntimeSource;
    private int lastKnownHealth = -1;
    private bool lastKnownDead;
    private bool sharedModeHealthReady;
    private float nextSharedModeHealthProbeTime;
    private const float SharedModeHealthProbeInterval = 0.25f;
    private bool groundedStateInitialized;
    private bool wasGrounded;
    private float airborneStartTime;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        playerAttack = GetComponent<PlayerAttack>();
        playerHealth = GetComponent<PlayerHealth>();
        sharedModeController = GetComponent<SharedModePlayerController>();
        characterController = GetComponent<CharacterController>();
        networkCharacterController = GetComponent<Fusion.NetworkCharacterController>();
        EnsureAudioSourceReady();
        EnsureDedicatedRuntimeSource();
        RefreshCharacterTypeFromPlayerScripts();
        CacheHealthState(forceRefresh: true);
    }

    private void OnEnable()
    {
        sharedModeHealthReady = false;
        nextSharedModeHealthProbeTime = 0f;
        groundedStateInitialized = false;
        wasGrounded = false;
        airborneStartTime = 0f;
        RefreshCharacterTypeFromPlayerScripts();
    }

    private void Update()
    {
        CheckHealthAudioState();
        CheckJumpLandAudioState();

        if (!useMouseInput)
        {
            return;
        }

        RefreshCharacterTypeFromPlayerScripts();

        if (IsBowMode())
        {
            HandleBowMouseInput();
        }
        else
        {
            HandleMeleeMouseInput();
        }
    }

    private void HandleMeleeMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            bool comboExpired = (Time.time - lastMeleeClickTime) > meleeComboResetSeconds;
            if (comboExpired)
            {
                meleeComboStep = 0;
            }

            meleeComboStep = Mathf.Clamp(meleeComboStep + 1, 1, 3);
            lastMeleeClickTime = Time.time;

            PlayClip(GetMeleeClipByStep(meleeComboStep), "MeleeMouseClick");

            if (meleeComboStep >= 3)
            {
                meleeComboStep = 0;
            }
        }

        if ((Time.time - lastMeleeClickTime) > meleeComboResetSeconds)
        {
            meleeComboStep = 0;
        }
    }

    private void HandleBowMouseInput()
    {
        bool aimHeld = Input.GetMouseButton(1);

        if (Input.GetMouseButtonDown(1) || (aimHeld && !wasAimHeld))
        {
            PlayClip(bowDraw, "BowAimStart");
        }

        if (Input.GetMouseButtonDown(0))
        {
            PlayClip(bowShoot, "BowShoot");
        }

        wasAimHeld = aimHeld;
    }

    private void CheckHealthAudioState()
    {
        if (!TryGetCharacterHealthState(out int currentHealth, out bool isDead))
        {
            return;
        }

        if (lastKnownHealth < 0)
        {
            CacheHealthState(forceRefresh: true);
            return;
        }

        if (isDead && !lastKnownDead)
        {
            PlayClip(deathSound, "DeathSound");
            CacheHealthState(forceRefresh: true);
            return;
        }

        if (currentHealth < lastKnownHealth)
        {
            PlayClip(hurtSound, "HurtSound");
        }

        CacheHealthState(forceRefresh: false);
    }

    private void CheckJumpLandAudioState()
    {
        if (!autoPlayJumpLandByGroundedState)
        {
            return;
        }

        if (!TryGetGroundedState(out bool isGroundedNow))
        {
            return;
        }

        if (!groundedStateInitialized)
        {
            groundedStateInitialized = true;
            wasGrounded = isGroundedNow;
            airborneStartTime = isGroundedNow ? 0f : Time.time;
            return;
        }

        if (wasGrounded && !isGroundedNow)
        {
            airborneStartTime = Time.time;
            if (playJumpSoundWhenLeaveGround)
            {
                PlayClip(jumpUpSound, "JumpUpSound");
            }
        }
        else if (!wasGrounded && isGroundedNow)
        {
            float airTime = Time.time - airborneStartTime;
            if (playLandSoundWhenTouchGround && airTime >= minAirborneTimeForLandSound)
            {
                PlayClip(landSound, "LandSound");
            }
        }

        wasGrounded = isGroundedNow;
    }

    private bool TryGetGroundedState(out bool grounded)
    {
        grounded = false;

        if (networkCharacterController != null)
        {
            grounded = networkCharacterController.Grounded;
            return true;
        }

        if (characterController != null)
        {
            grounded = characterController.isGrounded;
            return true;
        }

        return false;
    }

    private void CacheHealthState(bool forceRefresh)
    {
        if (!TryGetCharacterHealthState(out int currentHealth, out bool isDead))
        {
            return;
        }

        if (forceRefresh || currentHealth >= 0)
        {
            lastKnownHealth = currentHealth;
            lastKnownDead = isDead;
        }
    }

    private bool TryGetCharacterHealthState(out int currentHealth, out bool isDead)
    {
        currentHealth = -1;
        isDead = false;

        if (sharedModeController != null)
        {
            if (!sharedModeHealthReady && Time.time < nextSharedModeHealthProbeTime)
            {
                return false;
            }

            try
            {
                currentHealth = sharedModeController.Health;
                isDead = sharedModeController.IsDead;
                sharedModeHealthReady = true;
                return true;
            }
            catch (System.Exception)
            {
                sharedModeHealthReady = false;
                nextSharedModeHealthProbeTime = Time.time + SharedModeHealthProbeInterval;
                return false;
            }
        }

        if (playerHealth != null)
        {
            currentHealth = playerHealth.CurrentHealth;
            isDead = playerHealth.IsDead;
            return true;
        }

        return false;
    }

    // Animation Event: play next melee combo sound (cycles 1 -> 2 -> 3).
    public void PlayMeleeAttackSfx()
    {
        PlayClip(GetMeleeClipByStep(1), "PlayMeleeAttackSfx");
    }

    // Animation Event with int parameter from timeline.
    public void PlayMeleeAttackByStepSfx(int comboStep)
    {
        PlayClip(GetMeleeClipByStep(comboStep), "PlayMeleeAttackByStepSfx");
    }

    // Animation Event: explicit clip helpers for melee combo.
    public void PlayMeleeAttack1Sfx()
    {
        PlayClip(meleeAttack1, "PlayMeleeAttack1Sfx");
    }

    public void PlayMeleeAttack2Sfx()
    {
        PlayClip(meleeAttack2, "PlayMeleeAttack2Sfx");
    }

    public void PlayMeleeAttack3Sfx()
    {
        PlayClip(meleeAttack3, "PlayMeleeAttack3Sfx");
    }

    // Animation Event: block/parry sound for melee class.
    public void PlayMeleeBlockSfx()
    {
        PlayClip(meleeBlock, "PlayMeleeBlockSfx");
    }

    // Animation Event: draw string / aim start sound for bow class.
    public void PlayBowDrawSfx()
    {
        PlayClip(bowDraw, "PlayBowDrawSfx");
    }

    // Animation Event: release / fire arrow sound for bow class.
    public void PlayBowShootSfx()
    {
        PlayClip(bowShoot, "PlayBowShootSfx");
    }

    // Animation Event: jump start sound.
    public void PlayJumpUpSfx()
    {
        PlayClip(jumpUpSound, "PlayJumpUpSfx");
    }

    // Animation Event: landing sound.
    public void PlayLandSfx()
    {
        PlayClip(landSound, "PlayLandSfx");
    }

    // Compatibility aliases for animation event naming flexibility (non-overloaded).
    public void MeleeAttackSfx() => PlayMeleeAttackSfx();
    public void MeleeAttackStepSfx(int comboStep) => PlayMeleeAttackByStepSfx(comboStep);
    public void MeleeBlockSfx() => PlayMeleeBlockSfx();
    public void BowDrawSfx() => PlayBowDrawSfx();
    public void BowShootSfx() => PlayBowShootSfx();
    public void JumpUpSfx() => PlayJumpUpSfx();
    public void LandSfx() => PlayLandSfx();

    private bool IsBowMode()
    {
        return characterType == CharacterType.Bow;
    }

    private AudioClip GetMeleeClipByStep(int comboStep)
    {
        switch (comboStep)
        {
            case 1:
                return meleeAttack1;
            case 2:
                return meleeAttack2;
            case 3:
                return meleeAttack3;
            default:
                return meleeAttack1;
        }
    }

    private void PlayClip(AudioClip clip, string eventName)
    {
        if (clip == null)
        {
            if (debugLogs)
            {
                Debug.LogWarning($"[SoundEffect] {eventName}: clip is null on {name}.", this);
            }

            return;
        }

        if (clip.loadState != AudioDataLoadState.Loaded)
        {
            clip.LoadAudioData();
            StartCoroutine(PlayClipWhenLoaded(clip, eventName, 1.5f));

            if (debugLogs)
            {
                Debug.LogWarning($"[SoundEffect] {eventName}: clip '{clip.name}' not loaded yet, waiting for load. Current state: {clip.loadState}.", this);
            }

            return;
        }

        AudioSource playbackSource = GetPlaybackSource();
        if (playbackSource == null)
        {
            if (debugLogs)
            {
                Debug.LogWarning($"[SoundEffect] {eventName}: no playback AudioSource on {name}.", this);
            }

            return;
        }

        EnsureAudioSourceReady();
        EnsureAudibleState(eventName);

        float pitchOffset = randomPitchRange > 0f ? Random.Range(-randomPitchRange, randomPitchRange) : 0f;
        playbackSource.pitch = Mathf.Clamp(1f + pitchOffset, 0.5f, 2f);
        playbackSource.PlayOneShot(clip, volumeMultiplier);

        if (useCameraFallbackPlayAtPoint)
        {
            Camera cam = Camera.main;
            Vector3 playPos = cam != null ? cam.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(clip, playPos, volumeMultiplier);
        }

        if (debugLogs)
        {
            Debug.Log($"[SoundEffect] {eventName}: played '{clip.name}' on {playbackSource.gameObject.name}.", this);
        }
    }

    private IEnumerator PlayClipWhenLoaded(AudioClip clip, string eventName, float timeoutSeconds)
    {
        float start = Time.realtimeSinceStartup;

        while (clip != null && clip.loadState == AudioDataLoadState.Loading)
        {
            if ((Time.realtimeSinceStartup - start) >= timeoutSeconds)
            {
                break;
            }

            yield return null;
        }

        if (clip == null)
        {
            yield break;
        }

        if (clip.loadState != AudioDataLoadState.Loaded)
        {
            if (debugLogs)
            {
                Debug.LogWarning($"[SoundEffect] {eventName}: clip '{clip.name}' failed to load, state={clip.loadState}.", this);
            }

            yield break;
        }

        AudioSource playbackSource = GetPlaybackSource();
        if (playbackSource == null)
        {
            if (debugLogs)
            {
                Debug.LogWarning($"[SoundEffect] {eventName}: no playback AudioSource after load for '{clip.name}'.", this);
            }

            yield break;
        }

        EnsureAudioSourceReady();
        EnsureAudibleState(eventName + "(Loaded)");

        float pitchOffset = randomPitchRange > 0f ? Random.Range(-randomPitchRange, randomPitchRange) : 0f;
        playbackSource.pitch = Mathf.Clamp(1f + pitchOffset, 0.5f, 2f);
        playbackSource.PlayOneShot(clip, volumeMultiplier);

        if (useCameraFallbackPlayAtPoint)
        {
            Camera cam = Camera.main;
            Vector3 playPos = cam != null ? cam.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(clip, playPos, volumeMultiplier);
        }

        if (debugLogs)
        {
            Debug.Log($"[SoundEffect] {eventName}: delayed-played '{clip.name}' on {playbackSource.gameObject.name}.", this);
        }
    }

    private void RefreshCharacterTypeFromPlayerScripts()
    {
        if (!autoDetectCharacterType)
        {
            return;
        }

        bool usesBow = false;
        if (playerAttack != null)
        {
            usesBow = playerAttack.UsesBow;
        }
        else if (sharedModeController != null)
        {
            usesBow = sharedModeController.UsesBow;
        }

        characterType = usesBow ? CharacterType.Bow : CharacterType.Melee;
    }

    private void EnsureAudioSourceReady()
    {
        if (audioSource == null)
        {
            return;
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        audioSource.bypassEffects = true;
        audioSource.bypassListenerEffects = true;
        audioSource.bypassReverbZones = true;
        audioSource.reverbZoneMix = 0f;
        audioSource.priority = 0;
        audioSource.volume = Mathf.Max(audioSource.volume, 1f);
        audioSource.mute = false;
        audioSource.outputAudioMixerGroup = null;
        audioSource.ignoreListenerVolume = true;
        audioSource.ignoreListenerPause = true;
    }

    private void EnsureDedicatedRuntimeSource()
    {
        if (!useDedicatedRuntimeSource || dedicatedRuntimeSource != null)
        {
            return;
        }

        GameObject sourceHost = new GameObject("SfxRuntimeSource");
        sourceHost.transform.SetParent(transform, false);
        dedicatedRuntimeSource = sourceHost.AddComponent<AudioSource>();
        dedicatedRuntimeSource.playOnAwake = false;
        dedicatedRuntimeSource.loop = false;
        dedicatedRuntimeSource.spatialBlend = 0f;
        dedicatedRuntimeSource.bypassEffects = true;
        dedicatedRuntimeSource.bypassListenerEffects = true;
        dedicatedRuntimeSource.bypassReverbZones = true;
        dedicatedRuntimeSource.reverbZoneMix = 0f;
        dedicatedRuntimeSource.priority = 0;
        dedicatedRuntimeSource.volume = 1f;
        dedicatedRuntimeSource.mute = false;
        dedicatedRuntimeSource.outputAudioMixerGroup = null;
        dedicatedRuntimeSource.ignoreListenerVolume = true;
        dedicatedRuntimeSource.ignoreListenerPause = true;
    }

    private AudioSource GetPlaybackSource()
    {
        if (useDedicatedRuntimeSource)
        {
            EnsureDedicatedRuntimeSource();
            if (dedicatedRuntimeSource != null)
            {
                return dedicatedRuntimeSource;
            }
        }

        return audioSource;
    }

    private void EnsureAudibleState(string eventName)
    {
        AudioSource playbackSource = GetPlaybackSource();
        if (!forceAudibleAtRuntime || playbackSource == null)
        {
            return;
        }

        if (AudioListener.pause)
        {
            AudioListener.pause = false;
            if (debugLogs)
            {
                Debug.LogWarning($"[SoundEffect] {eventName}: AudioListener.pause was true, forced to false.", this);
            }
        }

        if (AudioListener.volume <= 0.0001f)
        {
            AudioListener.volume = 1f;
            if (debugLogs)
            {
                Debug.LogWarning($"[SoundEffect] {eventName}: AudioListener.volume was 0, forced to 1.", this);
            }
        }

        if (playbackSource.mute)
        {
            playbackSource.mute = false;
            if (debugLogs)
            {
                Debug.LogWarning($"[SoundEffect] {eventName}: playback source mute was true, forced to false.", this);
            }
        }

        if (playbackSource.volume <= 0.0001f)
        {
            playbackSource.volume = fallbackSourceVolume;
            if (debugLogs)
            {
                Debug.LogWarning($"[SoundEffect] {eventName}: playback source volume was 0, forced to {fallbackSourceVolume:0.##}.", this);
            }
        }

        playbackSource.ignoreListenerPause = true;
    }

}
    