using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class KillFeedTracker : MonoBehaviour
{
    private struct PlayerSnapshot
    {
        public int killCount;
        public bool isDead;
        public PlayerRef playerRef;
    }

    private readonly Dictionary<int, PlayerSnapshot> snapshots = new Dictionary<int, PlayerSnapshot>();
    private readonly List<PlayerRef> killerEvents = new List<PlayerRef>();
    private readonly List<PlayerRef> victimEvents = new List<PlayerRef>();

    private void Update()
    {
        CollectEvents();
        PublishKillFeedMessages();
    }

    private void CollectEvents()
    {
        killerEvents.Clear();
        victimEvents.Clear();

        SharedModePlayerController[] controllers = FindObjectsByType<SharedModePlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        HashSet<int> liveKeys = new HashSet<int>();

        for (int i = 0; i < controllers.Length; i++)
        {
            SharedModePlayerController controller = controllers[i];
            if (controller == null || controller.Object == null || !controller.Object.IsValid)
            {
                continue;
            }

            PlayerRef playerRef = controller.Object.InputAuthority;
            if (!playerRef.IsRealPlayer)
            {
                continue;
            }

            int key = playerRef.RawEncoded;
            liveKeys.Add(key);

            int killCount = controller.KillCount;
            bool isDead = controller.IsDead;

            if (!snapshots.TryGetValue(key, out PlayerSnapshot previous))
            {
                snapshots[key] = new PlayerSnapshot
                {
                    killCount = killCount,
                    isDead = isDead,
                    playerRef = playerRef,
                };
                continue;
            }

            if (killCount > previous.killCount)
            {
                int delta = killCount - previous.killCount;
                for (int j = 0; j < delta; j++)
                {
                    killerEvents.Add(playerRef);
                }
            }

            if (!previous.isDead && isDead)
            {
                victimEvents.Add(playerRef);
            }

            previous.killCount = killCount;
            previous.isDead = isDead;
            previous.playerRef = playerRef;
            snapshots[key] = previous;
        }

        if (liveKeys.Count == snapshots.Count)
        {
            return;
        }

        List<int> missing = new List<int>();
        foreach (KeyValuePair<int, PlayerSnapshot> pair in snapshots)
        {
            if (!liveKeys.Contains(pair.Key))
            {
                missing.Add(pair.Key);
            }
        }

        for (int i = 0; i < missing.Count; i++)
        {
            snapshots.Remove(missing[i]);
        }
    }

    private void PublishKillFeedMessages()
    {
        int pairCount = Mathf.Min(killerEvents.Count, victimEvents.Count);
        for (int i = 0; i < pairCount; i++)
        {
            PlayerRef killer = killerEvents[i];
            PlayerRef victim = victimEvents[i];

            string killerName = ResolvePlayerName(killer, "Player");
            string victimName = ResolvePlayerName(victim, "Player");
            KillFeedUI.ShowMessage($"{killerName} kill {victimName}");
        }
    }

    private static string ResolvePlayerName(PlayerRef player, string fallback)
    {
        SharedRoomSessionManager sessionManager = SharedRoomSessionManager.Instance;
        return FusionLobbyNameResolver.ResolvePlayerName(sessionManager, player, -1, fallback);
    }
}
