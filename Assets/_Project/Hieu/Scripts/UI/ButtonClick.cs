using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonClick : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private Dotween settingsUI;
	[SerializeField] private GameObject startButton;
	[SerializeField] private GameObject settingsButton;
	[SerializeField] private GameObject exitButton;
	[SerializeField] private GameObject roomSelectionUI;
	[SerializeField] private GameObject classChooseUI;
	[SerializeField] private CanvasGroup roomSelectionCanvasGroup;

	[Header("Room Input")]
	[SerializeField] private TMP_InputField roomIdTmpInputField;
	[SerializeField] private InputField roomIdLegacyInputField;
	[SerializeField] private TMP_Text roomFeedbackTmpText;
	[SerializeField] private Text roomFeedbackLegacyText;

	private bool roomActionInProgress;

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

		SetRoomFeedback(string.Empty);

		SetMainMenuButtonsVisible(true);
	}

	public void OpenSettings()
	{
		if (settingsUI == null)
		{
			return;
		}

		SetMainMenuButtonsVisible(false);
		settingsUI.ShowSettings();
	}

	public void CloseSettings()
	{
		if (settingsUI == null)
		{
			return;
		}

		settingsUI.HideSettings();
		SetMainMenuButtonsVisible(true);
	}

	public void ToggleSettings()
	{
		if (settingsUI == null)
		{
			return;
		}

		bool isOpening = !settingsUI.IsSettingsVisible();
		settingsUI.ToggleSettings();
		SetMainMenuButtonsVisible(!isOpening);
	}

	public void OpenRoomSelectionUI()
	{
		if (roomSelectionUI == null)
		{
			return;
		}

		SetMainMenuButtonsVisible(false);
		roomSelectionUI.SetActive(true);
	}

	public void CloseRoomSelectionUI()
	{
		if (roomSelectionUI == null)
		{
			return;
		}

		roomSelectionUI.SetActive(false);
		SetMainMenuButtonsVisible(true);
	}

	public void ToggleRoomSelectionUI()
	{
		if (roomSelectionUI == null)
		{
			return;
		}

		roomSelectionUI.SetActive(!roomSelectionUI.activeSelf);
	}

	public async void OnCreateRoomClicked()
	{
		await HandleRoomActionAsync(isCreateAction: true);
	}

	public async void OnJoinRoomClicked()
	{
		await HandleRoomActionAsync(isCreateAction: false);
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

	private async Task HandleRoomActionAsync(bool isCreateAction)
	{
		if (roomActionInProgress)
		{
			return;
		}

		string roomId = GetRoomIdInput();
		if (string.IsNullOrWhiteSpace(roomId))
		{
			SetRoomFeedback("Hay nhap Room ID.");
			return;
		}

		SharedRoomSessionManager sessionManager = SharedRoomSessionManager.EnsureInstance();
		if (sessionManager == null)
		{
			SetRoomFeedback("Khong tao duoc Session Manager.");
			return;
		}

		roomActionInProgress = true;
		SetRoomSelectionInteractable(false);
		SetRoomFeedback(isCreateAction ? "Dang tao room..." : "Dang join room...");

		bool success = isCreateAction
			? await sessionManager.CreateRoomAsync(roomId)
			: await sessionManager.JoinRoomAsync(roomId);

		roomActionInProgress = false;
		SetRoomSelectionInteractable(true);

		if (!success)
		{
			SetRoomFeedback(isCreateAction ? "Tao room that bai." : "Join room that bai.");
			return;
		}

		SetRoomFeedback(string.Empty);
		OpenClassChooseUI();
	}

	private string GetRoomIdInput()
	{
		if (roomIdTmpInputField != null)
		{
			return roomIdTmpInputField.text;
		}

		if (roomIdLegacyInputField != null)
		{
			return roomIdLegacyInputField.text;
		}

		return string.Empty;
	}

	private void SetRoomSelectionInteractable(bool isInteractable)
	{
		if (roomSelectionCanvasGroup != null)
		{
			roomSelectionCanvasGroup.interactable = isInteractable;
			roomSelectionCanvasGroup.blocksRaycasts = isInteractable;
		}

		if (roomIdTmpInputField != null)
		{
			roomIdTmpInputField.interactable = isInteractable;
		}

		if (roomIdLegacyInputField != null)
		{
			roomIdLegacyInputField.interactable = isInteractable;
		}
	}

	private void SetRoomFeedback(string message)
	{
		if (roomFeedbackTmpText != null)
		{
			roomFeedbackTmpText.text = message;
		}

		if (roomFeedbackLegacyText != null)
		{
			roomFeedbackLegacyText.text = message;
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
