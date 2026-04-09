using UnityEngine;

public class ButtonClick : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private Dotween settingsUI;
	[SerializeField] private GameObject startButton;
	[SerializeField] private GameObject settingsButton;
	[SerializeField] private GameObject exitButton;
	[SerializeField] private GameObject roomSelectionUI;
	[SerializeField] private GameObject classChooseUI;

	private void Awake()
	{
		if (settingsUI == null)
		{
			settingsUI = FindFirstObjectByType<Dotween>();
		}
	}

	private void Start()
	{
		if (settingsUI != null)
		{
			settingsUI.HideSettingsImmediate();
		}

		if (roomSelectionUI != null)
		{
			roomSelectionUI.SetActive(false);
		}

		if (classChooseUI != null)
		{
			classChooseUI.SetActive(false);
		}

		SetMainMenuButtonsVisible(true);
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

	public void OpenRoomSelectionUI()
	{
		if (roomSelectionUI == null)
		{
			return;
		}

		roomSelectionUI.SetActive(true);
	}

	public void CloseRoomSelectionUI()
	{
		if (roomSelectionUI == null)
		{
			return;
		}

		roomSelectionUI.SetActive(false);
	}

	public void ToggleRoomSelectionUI()
	{
		if (roomSelectionUI == null)
		{
			return;
		}

		roomSelectionUI.SetActive(!roomSelectionUI.activeSelf);
	}

	public void OnCreateRoomClicked()
	{
		OpenClassChooseUI();
	}

	public void OnJoinRoomClicked()
	{
		OpenClassChooseUI();
	}

	private void OpenClassChooseUI()
	{
		if (roomSelectionUI != null)
		{
			roomSelectionUI.SetActive(false);
		}

		if (classChooseUI != null)
		{
			ClassChoose classChoose = classChooseUI.GetComponent<ClassChoose>();
			if (classChoose != null)
			{
				classChoose.ResetSelectionUI();
			}

			classChooseUI.SetActive(true);
		}
	}

	private void SetMainMenuButtonsVisible(bool isVisible)
	{
		if (startButton != null)
		{
			startButton.SetActive(isVisible);
		}

		if (settingsButton != null)
		{
			settingsButton.SetActive(isVisible);
		}

		if (exitButton != null)
		{
			exitButton.SetActive(isVisible);
		}
	}
}
