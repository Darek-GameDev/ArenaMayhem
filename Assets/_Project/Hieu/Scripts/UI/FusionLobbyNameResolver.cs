using Fusion;

public static class FusionLobbyNameResolver
{
    public static string ResolvePlayerName(SharedRoomSessionManager sessionManager, PlayerRef player, int slotIndex, string fallback)
    {
        if (sessionManager != null)
        {
            string managerName = sessionManager.GetPlayerName(player, string.Empty);
            if (!string.IsNullOrWhiteSpace(managerName))
            {
                return managerName;
            }
        }

        return fallback;
    }
}
