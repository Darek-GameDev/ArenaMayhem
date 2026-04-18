using Fusion;

public static class FusionLobbyNameResolver
{
    public static string ResolvePlayerName(SharedRoomSessionManager sessionManager, PlayerRef player, int slotIndex, string fallback)
    {
        if (sessionManager != null)
        {
            string resolved = sessionManager.GetPlayerName(player, string.Empty);
            if (!string.IsNullOrWhiteSpace(resolved))
            {
                return resolved;
            }
        }

        return fallback;
    }
}
