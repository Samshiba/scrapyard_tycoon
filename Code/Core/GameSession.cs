public static class GameSession
{
    public static string TargetFactoryId { get; set; }
    public static bool IsNewGame { get; set; }
    public static string NewSaveName { get; set; }

    public static void Clear()
    {
        TargetFactoryId = null;
        IsNewGame = false;
        NewSaveName = "";
    }
}