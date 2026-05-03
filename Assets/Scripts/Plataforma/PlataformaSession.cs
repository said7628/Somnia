public static class PlataformaSession
{
    public static int SlotNumber = 1;
    public static int LevelId = 0;
    public static string SourceLevelScene = "";
    public static string ReturnScene = "Isla1";
    public static int LivesRemaining = 0;
    public static int MaxLives = 0;
    public static bool Completed = false;
    public static bool RewardAlreadySaved = false;

    public static bool IsPerfect => MaxLives > 0 && LivesRemaining >= MaxLives;
    public static int BaseReward => 50;
    public static int PerfectBonus => IsPerfect ? 10 : 0;
    public static int TotalReward => BaseReward + PerfectBonus;
}