using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;

public class SkillCooldown : MonoBehaviour
{
	[Header("Fusion")]
	[SerializeField] private NetworkRunner runner;
	[SerializeField] private bool autoFindRunner = true;

	[Header("UI")]
	[SerializeField] private GameObject cooldownObject;
	[SerializeField] private Image cooldownFillImage;
	[SerializeField] private TMP_Text cooldownText;
	[SerializeField] private Text cooldownTextLegacy;

	[Header("Display")]
	[SerializeField] [Min(0.01f)] private float cooldownDuration = 5f;
	[SerializeField] private bool hideWhenReady = true;
	[SerializeField] private bool counterClockwise = true;
	[SerializeField] private Image.Origin360 radialOrigin = Image.Origin360.Top;
	[SerializeField] private bool useLocalTimeWhenRunnerMissing = true;

	[Header("Debug")]
	[SerializeField] private bool enableTestKey = true;
	[SerializeField] private KeyCode testKey = KeyCode.P;

	private float cooldownEndSimulationTime;
	private float cooldownEndLocalTime;
	private bool isCoolingDown;
	private int lastDisplayedSeconds = int.MinValue;
	private bool lastVisibleState;
	private bool hasConfiguredFill;

	private void Awake()
	{
		if (cooldownFillImage == null)
		{
			cooldownFillImage = GetComponent<Image>();
		}

		ConfigureFillImage();
		ApplyUI(0f, false);
	}

	private void Update()
	{
		ResolveRunner();

		if (enableTestKey && Input.GetKeyDown(testKey))
		{
			StartCooldown(cooldownDuration);
		}

		if (!isCoolingDown)
		{
			ApplyUI(0f, false);
			return;
		}

		float remaining;
		if (runner != null && runner.IsRunning)
		{
			float simulationTime = (float)runner.SimulationTime;
			remaining = Mathf.Max(0f, cooldownEndSimulationTime - simulationTime);
		}
		else if (useLocalTimeWhenRunnerMissing)
		{
			remaining = Mathf.Max(0f, cooldownEndLocalTime - Time.unscaledTime);
		}
		else
		{
			ApplyUI(0f, false);
			return;
		}

		bool isOnCooldown = remaining > 0.0001f;
		if (!isOnCooldown)
		{
			isCoolingDown = false;
		}

		ApplyUI(remaining, isOnCooldown);
	}

	public void StartCooldown(float durationSeconds)
	{
		cooldownDuration = Mathf.Max(0.01f, durationSeconds);
		TryResolveRunnerAtRuntime();

		if (runner != null && runner.IsRunning)
		{
			float now = (float)runner.SimulationTime;
			cooldownEndLocalTime = 0f;
			SetCooldownEndTime(now + cooldownDuration);
			return;
		}

		if (useLocalTimeWhenRunnerMissing)
		{
			isCoolingDown = true;
			cooldownEndSimulationTime = 0f;
			cooldownEndLocalTime = Time.unscaledTime + cooldownDuration;
			ApplyUI(cooldownDuration, true);
			return;
		}

		ApplyUI(0f, false);
	}

	public void SetCooldownEndTime(float endSimulationTime)
	{
		cooldownEndSimulationTime = endSimulationTime;
		cooldownEndLocalTime = 0f;
		isCoolingDown = true;
		ApplyUI(GetRemainingCooldown(), true);
	}

	public void SyncFromNetwork(float endSimulationTime, float durationSeconds)
	{
		cooldownDuration = Mathf.Max(0.01f, durationSeconds);
		SetCooldownEndTime(endSimulationTime);
	}

	public void StopCooldown()
	{
		isCoolingDown = false;
		cooldownEndSimulationTime = 0f;
		cooldownEndLocalTime = 0f;
		ApplyUI(0f, false);
	}

	public float GetRemainingCooldown()
	{
		ResolveRunner();

		if (runner != null && runner.IsRunning)
		{
			float simulationTime = (float)runner.SimulationTime;
			return Mathf.Max(0f, cooldownEndSimulationTime - simulationTime);
		}

		if (useLocalTimeWhenRunnerMissing && isCoolingDown)
		{
			return Mathf.Max(0f, cooldownEndLocalTime - Time.unscaledTime);
		}

		return 0f;
	}

	private void ResolveRunner()
	{
		if (!autoFindRunner)
		{
			return;
		}

		if (runner != null && runner.IsRunning)
		{
			return;
		}

		runner = FindFirstObjectByType<NetworkRunner>(FindObjectsInactive.Exclude);
	}

	private void TryResolveRunnerAtRuntime()
	{
		if (runner != null && runner.IsRunning)
		{
			return;
		}

		ResolveRunner();
	}

	private void ConfigureFillImage()
	{
		if (cooldownFillImage == null)
		{
			return;
		}

		cooldownFillImage.type = Image.Type.Filled;
		cooldownFillImage.fillMethod = Image.FillMethod.Radial360;
		cooldownFillImage.fillClockwise = !counterClockwise;
		cooldownFillImage.fillOrigin = (int)radialOrigin;
		hasConfiguredFill = true;
	}

	private void ApplyUI(float remaining, bool isOnCooldown)
	{
		if (!hasConfiguredFill)
		{
			ConfigureFillImage();
		}

		bool shouldShow = isOnCooldown || !hideWhenReady;
		if (shouldShow != lastVisibleState)
		{
			lastVisibleState = shouldShow;
			if (cooldownObject != null)
			{
				cooldownObject.SetActive(shouldShow);
			}
		}

		float fill = cooldownDuration > 0f ? Mathf.Clamp01(remaining / cooldownDuration) : 0f;
		if (cooldownFillImage != null)
		{
			cooldownFillImage.fillAmount = fill;
		}

		int displaySeconds = Mathf.CeilToInt(Mathf.Max(0f, remaining));
		if (displaySeconds == lastDisplayedSeconds && shouldShow == lastVisibleState)
		{
			return;
		}

		lastDisplayedSeconds = displaySeconds;
		string display = isOnCooldown && displaySeconds > 0 ? displaySeconds.ToString() : string.Empty;

		if (cooldownText != null)
		{
			cooldownText.text = display;
		}

		if (cooldownTextLegacy != null)
		{
			cooldownTextLegacy.text = display;
		}
	}

}
