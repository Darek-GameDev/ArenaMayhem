using UnityEngine;
using TMPro;
using System.Collections;
using DG.Tweening;

public class KillFeed : MonoBehaviour
{
	private static KillFeed instance;
	private static float lastReportTime;
	private static string lastReportKiller;
	private static string lastReportVictim;

	[Header("UI")]
	[SerializeField] private GameObject killFeedObject;
	[SerializeField] private TMP_Text killerNameTMP;
	[SerializeField] private TMP_Text victimNameTMP;
	[SerializeField] private bool autoAttachPvpObserver = true;

	[Header("Display")]
	[SerializeField] private float visibleDuration = 2f;
	[SerializeField] private float fadeInDuration = 0.12f;
	[SerializeField] private float fadeOutDuration = 0.2f;
	[SerializeField] private float popStartScale = 0.9f;
	[SerializeField] private float popShowDuration = 0.18f;
	[SerializeField] private Ease popShowEase = Ease.OutBack;
	[SerializeField] private Ease popHideEase = Ease.InBack;

	[Header("Debug")]
	[SerializeField] private bool enableTestKey = true;
	[SerializeField] private KeyCode testKey = KeyCode.L;
	[SerializeField] private string testKillerName = "Player A";
	[SerializeField] private string testVictimName = "Player B";

	private Coroutine hideRoutine;
	private CanvasGroup killFeedCanvasGroup;
	private RectTransform killFeedRectTransform;
	private Tween currentFadeTween;

	private void Awake()
	{
		if (instance != null && instance != this)
		{
			Destroy(gameObject);
			return;
		}

		instance = this;

		if (killFeedObject == null)
		{
			killFeedObject = gameObject;
		}

		if (autoAttachPvpObserver && GetComponent<KillFeedPvPObserver>() == null)
		{
			gameObject.AddComponent<KillFeedPvPObserver>();
		}

		killFeedCanvasGroup = killFeedObject.GetComponent<CanvasGroup>();
		if (killFeedCanvasGroup == null)
		{
			killFeedCanvasGroup = killFeedObject.AddComponent<CanvasGroup>();
		}

		killFeedRectTransform = killFeedObject.GetComponent<RectTransform>();

		if (killerNameTMP == null || victimNameTMP == null)
		{
			AutoResolveNameLabels();
		}

		HideImmediate();
	}

	private void Update()
	{
		if (enableTestKey && Input.GetKeyDown(testKey))
		{
			ShowKill(testKillerName, testVictimName);
		}
	}

	private void OnDestroy()
	{
		if (instance == this)
		{
			instance = null;
		}
	}

	public static void ReportKill(string killerName, string victimName)
	{
		string dedupeKiller = string.IsNullOrWhiteSpace(killerName) ? string.Empty : killerName.Trim();
		string dedupeVictim = string.IsNullOrWhiteSpace(victimName) ? string.Empty : victimName.Trim();
		float now = Time.unscaledTime;
		if (now - lastReportTime <= 0.2f && dedupeKiller == lastReportKiller && dedupeVictim == lastReportVictim)
		{
			return;
		}

		lastReportTime = now;
		lastReportKiller = dedupeKiller;
		lastReportVictim = dedupeVictim;

		if (instance == null)
		{
			instance = FindFirstObjectByType<KillFeed>(FindObjectsInactive.Include);
		}

		if (instance == null)
		{
			return;
		}

		instance.ShowKill(killerName, victimName);
	}

	public void ShowKill(string killerName, string victimName)
	{
		string safeKiller = string.IsNullOrWhiteSpace(killerName) ? "Player" : killerName.Trim();
		string safeVictim = string.IsNullOrWhiteSpace(victimName) ? "Player" : victimName.Trim();

		if (killerNameTMP != null)
		{
			killerNameTMP.text = safeKiller;
		}

		if (victimNameTMP != null)
		{
			victimNameTMP.text = safeVictim;
		}

		if (killFeedObject != null)
		{
			killFeedObject.SetActive(true);
		}

		currentFadeTween?.Kill();
		if (killFeedCanvasGroup != null)
		{
			killFeedCanvasGroup.alpha = 0f;
		}

		if (killFeedRectTransform != null)
		{
			killFeedRectTransform.localScale = Vector3.one * Mathf.Max(0.01f, popStartScale);
		}

		Sequence showSequence = DOTween.Sequence();
		if (killFeedCanvasGroup != null)
		{
			showSequence.Join(killFeedCanvasGroup.DOFade(1f, fadeInDuration));
		}

		if (killFeedRectTransform != null)
		{
			showSequence.Join(killFeedRectTransform.DOScale(1f, popShowDuration).SetEase(popShowEase));
		}

		currentFadeTween = showSequence;

		if (hideRoutine != null)
		{
			StopCoroutine(hideRoutine);
		}

		hideRoutine = StartCoroutine(HideAfterDelay());
	}

	public void HideImmediate()
	{
		if (hideRoutine != null)
		{
			StopCoroutine(hideRoutine);
			hideRoutine = null;
		}

		currentFadeTween?.Kill();
		currentFadeTween = null;

		if (killFeedCanvasGroup != null)
		{
			killFeedCanvasGroup.alpha = 0f;
		}

		if (killFeedRectTransform != null)
		{
			killFeedRectTransform.localScale = Vector3.one;
		}

		if (killFeedObject != null)
		{
			killFeedObject.SetActive(false);
		}
	}

	private IEnumerator HideAfterDelay()
	{
		yield return new WaitForSeconds(visibleDuration);

		if (killFeedCanvasGroup != null)
		{
			currentFadeTween?.Kill();

			Sequence hideSequence = DOTween.Sequence();
			hideSequence.Join(killFeedCanvasGroup.DOFade(0f, fadeOutDuration));
			if (killFeedRectTransform != null)
			{
				hideSequence.Join(killFeedRectTransform.DOScale(Mathf.Max(0.01f, popStartScale), fadeOutDuration).SetEase(popHideEase));
			}

			currentFadeTween = hideSequence;
			yield return currentFadeTween.WaitForCompletion();
		}

		HideImmediate();
	}

	private void OnDisable()
	{
		currentFadeTween?.Kill();
		currentFadeTween = null;
	}

	private void AutoResolveNameLabels()
	{
		TMP_Text[] labels = GetComponentsInChildren<TMP_Text>(true);
		for (int i = 0; i < labels.Length; i++)
		{
			TMP_Text label = labels[i];
			if (label == null)
			{
				continue;
			}

			string lowerName = label.name.ToLowerInvariant();
			if (killerNameTMP == null && (lowerName.Contains("playera") || lowerName.Contains("player_a") || lowerName.Contains("killer")))
			{
				killerNameTMP = label;
				continue;
			}

			if (victimNameTMP == null && (lowerName.Contains("playerb") || lowerName.Contains("player_b") || lowerName.Contains("victim")))
			{
				victimNameTMP = label;
			}
		}

		if (killerNameTMP == null && labels.Length > 0)
		{
			killerNameTMP = labels[0];
		}

		if (victimNameTMP == null && labels.Length > 1)
		{
			victimNameTMP = labels[labels.Length - 1];
		}
	}

}
