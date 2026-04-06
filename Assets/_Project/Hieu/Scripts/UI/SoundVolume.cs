using UnityEngine;
using UnityEngine.UI;

public class SoundVolume : MonoBehaviour
{
	private const string MenuMusicVolumeKey = "MenuMusicVolume";

	[Header("References")]
	[SerializeField] private Slider volumeSlider;
	[SerializeField] private AudioSource menuMusicSource;

	[Header("Defaults")]
	[SerializeField] [Range(0f, 1f)] private float defaultVolume = 0.75f;

	private void Awake()
	{
		if (volumeSlider == null)
		{
			volumeSlider = GetComponent<Slider>();
		}

		if (menuMusicSource == null)
		{
			menuMusicSource = FindFirstObjectByType<AudioSource>();
		}

		float savedVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MenuMusicVolumeKey, defaultVolume));

		if (volumeSlider != null)
		{
			volumeSlider.minValue = 0f;
			volumeSlider.maxValue = 1f;
			volumeSlider.SetValueWithoutNotify(savedVolume);
			volumeSlider.onValueChanged.AddListener(OnSliderValueChanged);
		}

		ApplyVolume(savedVolume);
	}

	private void OnDestroy()
	{
		if (volumeSlider != null)
		{
			volumeSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
		}
	}

	public void OnSliderValueChanged(float value)
	{
		float volume = Mathf.Clamp01(value);
		ApplyVolume(volume);
		PlayerPrefs.SetFloat(MenuMusicVolumeKey, volume);
		PlayerPrefs.Save();
	}

	public void SetMenuMusicSource(AudioSource source)
	{
		menuMusicSource = source;
		if (menuMusicSource == null)
		{
			return;
		}

		float savedVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MenuMusicVolumeKey, defaultVolume));
		menuMusicSource.volume = savedVolume;
	}

	private void ApplyVolume(float volume)
	{
		if (menuMusicSource != null)
		{
			menuMusicSource.volume = volume;
		}
	}
}
