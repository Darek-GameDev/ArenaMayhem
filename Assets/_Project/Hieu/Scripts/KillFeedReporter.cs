using System.Collections.Generic;
using Fusion;
using UnityEngine;

public static class KillFeedReporter
{
    private static readonly Dictionary<int, string> cachedLobbyNames = new Dictionary<int, string>();

    public static void ReportPlayerKill(NetworkRunner runner, PlayerRef attackerRef, PlayerRef victimRef)
    {
        string killerName = GetPlayerDisplayName(runner, attackerRef, $"Player {attackerRef.RawEncoded}");
        string victimName = GetPlayerDisplayName(runner, victimRef, $"Player {victimRef.RawEncoded}");
        KillFeed.ReportKill(killerName, victimName);
    }

    public static void ReportEnemyKill(NetworkRunner runner, PlayerRef attackerRef, string victimName)
    {
        string killerName = GetPlayerDisplayName(runner, attackerRef, $"Player {attackerRef.RawEncoded}");
        string safeVictimName = string.IsNullOrWhiteSpace(victimName) ? "Enemy" : victimName.Trim();
        KillFeed.ReportKill(killerName, safeVictimName);
    }

    private static string GetPlayerDisplayName(NetworkRunner runner, PlayerRef playerRef, string fallback)
    {
        if (!playerRef.IsRealPlayer)
        {
            return fallback;
        }

        int playerKey = playerRef.RawEncoded;

        SharedRoomSessionManager sessionManager = SharedRoomSessionManager.Instance ?? SharedRoomSessionManager.EnsureInstance();
        if (sessionManager != null)
        {
            string playerName = sessionManager.GetPlayerName(playerRef, string.Empty);
            if (!string.IsNullOrWhiteSpace(playerName))
            {
                string normalizedName = playerName.Trim();
                cachedLobbyNames[playerKey] = normalizedName;
                return normalizedName;
            }

            if (sessionManager.IsLocalPlayerOwner())
            {
                var players = sessionManager.GetPlayersOrderedById();
                for (int i = 0; i < players.Count; i++)
                {
                    if (players[i] != playerRef)
                    {
                        continue;
                    }

                    if (sessionManager.TryGetPlayerProfileBySlot(i + 1, out _, out string slotPlayerName) && !string.IsNullOrWhiteSpace(slotPlayerName))
                    {
                        string normalizedName = slotPlayerName.Trim();
                        cachedLobbyNames[playerKey] = normalizedName;
                        return normalizedName;
                    }

                    break;
                }
            }
        }

        if (cachedLobbyNames.TryGetValue(playerKey, out string cachedName) && !string.IsNullOrWhiteSpace(cachedName))
        {
            return cachedName;
        }

        if (runner != null && runner.LocalPlayer == playerRef)
        {
            string localName = PlayerPrefs.GetString("PLAYER_DISPLAY_NAME", string.Empty);
            if (!string.IsNullOrWhiteSpace(localName))
            {
                string normalizedName = localName.Trim();
                cachedLobbyNames[playerKey] = normalizedName;
                return normalizedName;
            }
        }

        return fallback;
    }
}