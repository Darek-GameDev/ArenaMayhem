using System.Collections.Generic;
using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
public class KillFeedPvPObserver : MonoBehaviour
{
    [SerializeField] private float scanInterval = 0.1f;

    private readonly Dictionary<int, int> lastKillCounts = new Dictionary<int, int>();
    private readonly Dictionary<int, bool> lastDeadStates = new Dictionary<int, bool>();
    private readonly Dictionary<int, int> killerCredits = new Dictionary<int, int>();

    private float nextScanTime;

    private void OnEnable()
    {
        CaptureSnapshot();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextScanTime)
        {
            return;
        }

        nextScanTime = Time.unscaledTime + Mathf.Max(0.02f, scanInterval);
        ScanAndReport();
    }

    [ContextMenu("Capture Snapshot")]
    public void CaptureSnapshot()
    {
        lastKillCounts.Clear();
        lastDeadStates.Clear();
        killerCredits.Clear();

        SharedModePlayerController[] players = FindPlayers();
        for (int i = 0; i < players.Length; i++)
        {
            SharedModePlayerController player = players[i];
            if (!TryGetPlayerKey(player, out int key))
            {
                continue;
            }

            lastKillCounts[key] = player.KillCount;
            lastDeadStates[key] = player.IsDead;
        }
    }

    private void ScanAndReport()
    {
        SharedModePlayerController[] players = FindPlayers();
        Dictionary<int, SharedModePlayerController> playersByKey = new Dictionary<int, SharedModePlayerController>();
        List<SharedModePlayerController> newlyDeadVictims = new List<SharedModePlayerController>();

        for (int i = 0; i < players.Length; i++)
        {
            SharedModePlayerController player = players[i];
            if (!TryGetPlayerKey(player, out int key))
            {
                continue;
            }

            playersByKey[key] = player;

            int currentKillCount = player.KillCount;
            int previousKillCount = lastKillCounts.TryGetValue(key, out int prevKills) ? prevKills : currentKillCount;
            int delta = currentKillCount - previousKillCount;
            if (delta > 0)
            {
                killerCredits[key] = killerCredits.TryGetValue(key, out int credits) ? credits + delta : delta;
            }

            bool currentDead = player.IsDead;
            bool wasDead = lastDeadStates.TryGetValue(key, out bool prevDead) && prevDead;
            if (currentDead && !wasDead)
            {
                newlyDeadVictims.Add(player);
            }

            lastKillCounts[key] = currentKillCount;
            lastDeadStates[key] = currentDead;
        }

        for (int i = 0; i < newlyDeadVictims.Count; i++)
        {
            SharedModePlayerController victim = newlyDeadVictims[i];
            if (!TryGetPlayerKey(victim, out int victimKey))
            {
                continue;
            }

            if (!TrySelectKillerKey(killerCredits, victimKey, out int killerKey))
            {
                continue;
            }

            if (!playersByKey.TryGetValue(killerKey, out SharedModePlayerController killer) || killer == null)
            {
                continue;
            }

            PlayerRef killerRef = killer.Object.InputAuthority;
            PlayerRef victimRef = victim.Object.InputAuthority;
            NetworkRunner runner = killer.Runner != null ? killer.Runner : victim.Runner;

            KillFeedReporter.ReportPlayerKill(runner, killerRef, victimRef);
        }

        PruneCredits();
    }

    private static bool TrySelectKillerKey(Dictionary<int, int> pendingKillerDeltas, int victimKey, out int killerKey)
    {
        killerKey = int.MinValue;

        int candidateCount = 0;
        int singleCandidateKey = int.MinValue;

        foreach (KeyValuePair<int, int> pair in pendingKillerDeltas)
        {
            if (pair.Key == victimKey || pair.Value <= 0)
            {
                continue;
            }

            candidateCount++;
            singleCandidateKey = pair.Key;
        }

        if (candidateCount == 1)
        {
            pendingKillerDeltas[singleCandidateKey] = pendingKillerDeltas[singleCandidateKey] - 1;
            killerKey = singleCandidateKey;
            return true;
        }

        int bestKey = int.MinValue;
        int bestDelta = 0;

        foreach (KeyValuePair<int, int> pair in pendingKillerDeltas)
        {
            if (pair.Key == victimKey || pair.Value <= 0)
            {
                continue;
            }

            // Keep behavior deterministic when deltas are tied: choose the smaller PlayerRef key.
            if (pair.Value > bestDelta || (pair.Value == bestDelta && (bestKey == int.MinValue || pair.Key < bestKey)))
            {
                bestDelta = pair.Value;
                bestKey = pair.Key;
            }
        }

        if (bestKey == int.MinValue)
        {
            return false;
        }

        pendingKillerDeltas[bestKey] = pendingKillerDeltas[bestKey] - 1;
        killerKey = bestKey;
        return true;
    }

    private void PruneCredits()
    {
        if (killerCredits.Count == 0)
        {
            return;
        }

        List<int> keysToRemove = null;
        foreach (KeyValuePair<int, int> pair in killerCredits)
        {
            if (pair.Value > 0)
            {
                continue;
            }

            if (keysToRemove == null)
            {
                keysToRemove = new List<int>();
            }

            keysToRemove.Add(pair.Key);
        }

        if (keysToRemove == null)
        {
            return;
        }

        for (int i = 0; i < keysToRemove.Count; i++)
        {
            killerCredits.Remove(keysToRemove[i]);
        }
    }

    private static SharedModePlayerController[] FindPlayers()
    {
        return FindObjectsByType<SharedModePlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
    }

    private static bool TryGetPlayerKey(SharedModePlayerController player, out int key)
    {
        key = int.MinValue;
        if (player == null || player.Object == null || !player.Object.InputAuthority.IsRealPlayer)
        {
            return false;
        }

        key = player.Object.InputAuthority.RawEncoded;
        return true;
    }
}
