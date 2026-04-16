using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CooldownScripts : MonoBehaviour
{
	[Header("Cooldown Settings")]
	[SerializeField] private float cooldownDuration = 5f;

	[Header("UI References")]
	[SerializeField] private GameObject cooldownObject;
	[SerializeField] private Image cooldownFillImage;
	[SerializeField] private TMP_Text cooldownTextTMP;
	[SerializeField] private Text cooldownTextLegacy;

	private float remainingTime;
	private bool isCoolingDown;

	private void Awake()
	{
		remainingTime = 0f;
		isCoolingDown = false;
		SetCooldownVisible(false);
		UpdateCooldownUI(0f);
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.E))
		{
			TryUseSkill();
		}

		if (!isCoolingDown)
		{
			return;
		}

		remainingTime -= Time.deltaTime;

		if (remainingTime <= 0f)
		{
			remainingTime = 0f;
			isCoolingDown = false;
			UpdateCooldownUI(0f);
			SetCooldownVisible(false);
			return;
		}

		UpdateCooldownUI(remainingTime);
	}

	public void TryUseSkill()
	{
		if (isCoolingDown)
		{
			return;
		}

		// Demo trigger: press E to consume skill and start cooldown.
		StartCooldown();
	}

	private void StartCooldown()
	{
		isCoolingDown = true;
		remainingTime = cooldownDuration;

		SetCooldownVisible(true);
		UpdateCooldownUI(remainingTime);
	}

	private void UpdateCooldownUI(float timeLeft)
	{
		float fillAmount = cooldownDuration > 0f ? Mathf.Clamp01(timeLeft / cooldownDuration) : 0f;

		if (cooldownFillImage != null)
		{
			cooldownFillImage.fillAmount = fillAmount;
		}

		int displaySeconds = Mathf.CeilToInt(Mathf.Max(0f, timeLeft));
		string displayText = displaySeconds.ToString();

		if (cooldownTextTMP != null)
		{
			cooldownTextTMP.text = displayText;
		}

		if (cooldownTextLegacy != null)
		{
			cooldownTextLegacy.text = displayText;
		}
	}

	private void SetCooldownVisible(bool visible)
	{
		if (cooldownObject != null)
		{
			cooldownObject.SetActive(visible);
		}
	}

}
