using Fusion;

public enum SharedPlayerClassType : byte
{
    Unknown = 0,
    Knight = 1,
    Archer = 2,
}

public static class SharedPlayerClassTypeUtility
{
    public const string KnightName = "Knight";
    public const string ArcherName = "Archer";

    public static SharedPlayerClassType FromClassName(string className)
    {
        if (string.IsNullOrWhiteSpace(className))
        {
            return SharedPlayerClassType.Unknown;
        }

        string normalized = className.Trim();
        if (normalized == KnightName)
        {
            return SharedPlayerClassType.Knight;
        }

        if (normalized == ArcherName)
        {
            return SharedPlayerClassType.Archer;
        }

        return SharedPlayerClassType.Unknown;
    }

    public static string ToClassName(SharedPlayerClassType classType)
    {
        switch (classType)
        {
            case SharedPlayerClassType.Knight:
                return KnightName;
            case SharedPlayerClassType.Archer:
                return ArcherName;
            default:
                return string.Empty;
        }
    }

    public static bool IsSwordClass(SharedPlayerClassType classType)
    {
        return classType != SharedPlayerClassType.Archer;
    }

    public static SharedPlayerClassType ResolveFromSessionProperty(SessionProperty property, SharedPlayerClassType fallback)
    {
        int encoded = property;
        if (encoded == (int)SharedPlayerClassType.Knight)
        {
            return SharedPlayerClassType.Knight;
        }

        if (encoded == (int)SharedPlayerClassType.Archer)
        {
            return SharedPlayerClassType.Archer;
        }

        return fallback;
    }

    public static string GetClassPropertyKey(PlayerRef player)
    {
        return $"class_{player.RawEncoded}";
    }

    public static string GetReadyPropertyKey(PlayerRef player)
    {
        return $"ready_{player.RawEncoded}";
    }

    public static string GetPlayerStatePropertyKey(PlayerRef player)
    {
        return $"state_{player.RawEncoded}";
    }

    public static int EncodePlayerState(SharedPlayerClassType classType, bool isReady)
    {
        int encodedClass = ((int)classType) & 0x3;
        return (encodedClass << 1) | (isReady ? 1 : 0);
    }

    public static SharedPlayerClassType DecodePlayerClass(int encodedState, SharedPlayerClassType fallback)
    {
        int encodedClass = (encodedState >> 1) & 0x3;
        if (encodedClass == (int)SharedPlayerClassType.Knight)
        {
            return SharedPlayerClassType.Knight;
        }

        if (encodedClass == (int)SharedPlayerClassType.Archer)
        {
            return SharedPlayerClassType.Archer;
        }

        return fallback;
    }

    public static bool DecodePlayerReady(int encodedState)
    {
        return (encodedState & 0x1) != 0;
    }
}
