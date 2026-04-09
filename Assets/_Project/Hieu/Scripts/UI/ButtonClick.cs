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

	// Auto-detect ClassChooseUI if not assigned
	private void OnEnable()
	{
		if (classChooseUI == null)
		{
			ClassChoose classChooseComponent = FindFirstObjectByType<ClassChoose>();
			if (classChooseComponent != null)
			{
				classChooseUI = classChooseComponent.gameObject;
				Debug.Log("[ButtonClick] ClassChooseUI auto-detected");
			}
		}

		if (roomSelectionUI == null)
		{
			GameObject roomSelection = GameObject.Find("RoomSelection") ?? GameObject.Find("RoomSelectionUI");
			if (roomSelection != null)
			{
				roomSelectionUI = roomSelection;
				Debug.Log("[ButtonClick] RoomSelectionUI auto-detected");
			}
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

		if (classChooseUI == null)
		{
			Debug.LogError("[ButtonClick.OpenClassChooseUI] classChooseUI is NULL.");
			return;
		}

		classChooseUI.SetActive(true);

		ClassChoose classChoose = classChooseUI.GetComponent<ClassChoose>();
		if (classChoose == null)
		{
			classChoose = classChooseUI.GetComponentInChildren<ClassChoose>(true);
		}

		if (classChoose != null)
		{
			classChoose.gameObject.SetActive(true);
			classChoose.ResetSelectionUI();
		}
		else
		{
			Debug.LogError("[ButtonClick.OpenClassChooseUI] Khong tim thay component ClassChoose.");
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
