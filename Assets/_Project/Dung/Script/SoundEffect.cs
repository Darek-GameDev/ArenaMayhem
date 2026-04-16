using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class SoundEffect : MonoBehaviour
{
    [Header("Melee")]
    [SerializeField] private AudioClip meleeAttackSound1;
    [SerializeField] private AudioClip meleeAttackSound2;
    [SerializeField] private AudioClip meleeAttackSound3;
    [SerializeField] private AudioClip meleeBlockSound;

    [Header("Bow")]
    [SerializeField] private AudioClip bowDrawSound;
    [SerializeField] private AudioClip bowShootSound;

    [Header("Movement")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip landSound;

    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 1f;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = soundVolume;
    }

    public void PlayMeleeAttackSound1()
    {
        PlaySound(meleeAttackSound1);
    }

    public void PlayMeleeAttackSound2()
    {
        PlaySound(meleeAttackSound2);
    }

    public void PlayMeleeAttackSound3()
    {
        PlaySound(meleeAttackSound3);
    }

    public void PlayMeleeBlockSound()
    {
        PlaySound(meleeBlockSound);
    }

    public void PlayBowDrawSound()
    {
        PlaySound(bowDrawSound);
    }

    public void PlayBowShootSound()
    {
        PlaySound(bowShootSound);
    }

    public void PlayJumpSound()
    {
        PlaySound(jumpSound);
    }

    public void PlayLandSound()
    {
        PlaySound(landSound);
    }

    public void PlaySound(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        audioSource.PlayOneShot(clip, soundVolume);
    }

    public void SetVolume(float volume)
    {
        soundVolume = Mathf.Clamp01(volume);
        if (audioSource != null)
        {
            audioSource.volume = soundVolume;
        }
    }
}
