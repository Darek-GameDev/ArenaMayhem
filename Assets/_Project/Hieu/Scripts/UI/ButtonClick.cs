using UnityEngine;

public class ButtonClick : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private Dotween settingsUI;

	private void Awake()
	{
		if (settingsUI == null)
		{
			settingsUI = FindFirstObjectByType<Dotween>();
		}
	}

	public void OpenSettings()
	{
		if (settingsUI == null)
		{
			return;
		}

		settingsUI.ShowSettings();
	}

	public void CloseSettings()
	{
		if (settingsUI == null)
		{
			return;
		}

		settingsUI.HideSettings();
	}

	public void ToggleSettings()
	{
		if (settingsUI == null)
		{
			return;
		}

		settingsUI.ToggleSettings();
	}
}
