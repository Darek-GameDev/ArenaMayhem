using UnityEngine;
using UnityEngine.UI;

public class ClickSound : MonoBehaviour
{
    [SerializeField] public AudioClip clickSoundClip;
    [SerializeField] public Slider soundVolumeSlider;
    private AudioSource audioSource;
    private float soundVolume = 1f;

    void Start()
    {
        // Lấy hoặc tạo AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Thiết lập slider nếu có
        if (soundVolumeSlider != null)
        {
            soundVolumeSlider.value = soundVolume;
            soundVolumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        audioSource.volume = soundVolume;
    }

    /// <summary>
    /// Phát âm thanh khi bấm nút
    /// </summary>
    public void PlayClickSound()
    {
        if (clickSoundClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSoundClip, soundVolume);
        }
    }

    /// <summary>
    /// Thay đổi âm lượng khi slider thay đổi
    /// </summary>
    private void OnVolumeChanged(float volume)
    {
        soundVolume = volume;
        if (audioSource != null)
        {
            audioSource.volume = soundVolume;
        }
    }

    /// <summary>
    /// Lấy âm lượng hiện tại
    /// </summary>
    public float GetVolume()
    {
        return soundVolume;
    }

    /// <summary>
    /// Thiết lập âm lượng trực tiếp
    /// </summary>
    public void SetVolume(float volume)
    {
        soundVolume = Mathf.Clamp01(volume);
        if (audioSource != null)
        {
            audioSource.volume = soundVolume;
        }
        if (soundVolumeSlider != null)
        {
            soundVolumeSlider.value = soundVolume;
        }
    }
}
