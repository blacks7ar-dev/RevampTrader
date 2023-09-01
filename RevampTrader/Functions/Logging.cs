namespace RevampTrader.Functions;

public static class Logging
{
    public static void LogDebug(string debug)
    {
        Plugin.RTLogger.LogDebug(debug);
    }

    public static void LogInfo(string info)
    {
        Plugin.RTLogger.LogInfo(info);
    }

    public static void LogWarning(string warning)
    {
        Plugin.RTLogger.LogWarning(warning);
    }

    public static void LogError(string error)
    {
        Plugin.RTLogger.LogError(error);
    }
}