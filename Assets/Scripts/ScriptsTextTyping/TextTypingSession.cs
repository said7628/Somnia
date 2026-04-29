using UnityEngine;
using UnityEngine.SceneManagement;

public static class TextTypingSession
{
    public static int EdadJugador = 0;
    public static int PlayerAge
    {
        get => EdadJugador;
        set => EdadJugador = Mathf.Max(0, value);
    }

    public static int LevelId = 0;
    public static string LevelName = "";

    public static int CurrentScore = 0;
    public static int MinimumScore = 0;
    public static int PersonalBest = 0;
    public static int PreviousPersonalBest = 0;

    public static int CalculatedYatzis = 0;
    public static int BaseYatzisEarned = 0;
    public static int AwardedYatzis = 0;
    public static int TotalYatzis = 0;
    public static bool RewardSavedInBackend = false;
    public static int PerfectBonusYatzis = 0;
    public static int ScoreBonusYatzis = 0;
    public static bool IsPerfect = false;
    public static bool WasPerfectRun = true;
    public static int MistakesCount = 0;
    // Backward-compatible alias for legacy scripts that still use TextTypingSession.Mistakes.
    public static int Mistakes
    {
        get => MistakesCount;
        set => MistakesCount = Mathf.Max(0, value);
    }
    public static int IslandId = 1;
    public static int SlotNumber = 1;

    public static bool Passed = false;
    public static bool WasPlayed = false;

    public static string LastLevelSceneName = "";
    public static int LastLevelId = 0;

    public static string SourceIslandSceneName = "Isla1";
    public static string EntryPointId = "";
    public static Vector3 ReturnPosition = Vector3.zero;
    public static Quaternion ReturnRotation = Quaternion.identity;
    public static bool HasReturnTransform = false;

    public static int EquippedColorItemId = 0;
    public static int EquippedEyesItemId = 0;
    public static int EquippedOutfitItemId = 0;

    public static void SetEquippedItems(int colorItemId, int eyesItemId, int outfitItemId)
    {
        EquippedColorItemId = colorItemId;
        EquippedEyesItemId = eyesItemId;
        EquippedOutfitItemId = outfitItemId;
    }

    public static void TrackLevelContext(int levelId, string sceneName)
    {
        LevelId = levelId;
        LevelName = sceneName;
        LastLevelId = levelId;
        LastLevelSceneName = sceneName;
    }

    public static void TrackReturnContext(string sourceIslandSceneName, Vector3 returnPosition, Quaternion returnRotation, string entryPointId = "")
    {
        SourceIslandSceneName = string.IsNullOrWhiteSpace(sourceIslandSceneName) ? ResolveCurrentIslandSceneFallback() : sourceIslandSceneName;
        ReturnPosition = returnPosition;
        ReturnRotation = returnRotation;
        EntryPointId = entryPointId ?? "";
        HasReturnTransform = true;
    }

    public static string ResolveReturnScene()
    {
        return string.IsNullOrWhiteSpace(SourceIslandSceneName)
            ? ResolveCurrentIslandSceneFallback()
            : SourceIslandSceneName;
    }

    private static string ResolveCurrentIslandSceneFallback()
    {
        string activeScene = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrWhiteSpace(activeScene) && activeScene.StartsWith("Isla"))
        {
            return activeScene;
        }

        return "Isla1";
    }

    public static int ResolveLevelIdFromSceneName(string sceneName, int fallbackLevelId = 1)
    {
        switch (sceneName)
        {
            case "TextTyping": return 1;
            case "TextTyping2": return 2;
            case "TextTyping3": return 3;
            case "TextTyping4": return 4;
            case "TextTyping5": return 5;
            case "TextTyping6": return 6;
            default: return Mathf.Max(1, fallbackLevelId);
        }
    }

    public static string ResolveSceneNameFromLevelId(int levelId)
    {
        switch (levelId)
        {
            case 1: return "TextTyping";
            case 2: return "TextTyping2";
            case 3: return "TextTyping3";
            case 4: return "TextTyping4";
            case 5: return "TextTyping5";
            case 6: return "TextTyping6";
            default: return "TextTyping";
        }
    }

    public static int GetPerfectBonusForIsland(int islandId)
    {
        switch (islandId)
        {
            case 1: return 50;
            case 2: return 55;
            default: return 50;
        }
    }

    public static int GetMaxBaseYatzisForIsland(int islandId)
    {
        switch (islandId)
        {
            case 1: return 200;
            case 2: return 220;
            default: return 200;
        }
    }

    public static int ResolveIslandId()
    {
        if (string.IsNullOrWhiteSpace(SourceIslandSceneName))
        {
            return 1;
        }

        string scene = SourceIslandSceneName.Trim();
        if (scene.StartsWith("Isla2"))
        {
            return 2;
        }

        return 1;
    }

    public static int CalculateBaseYatzisFromScore(int scoreFinal, float referenceMaxScore, int islandId)
    {
        float safeReference = referenceMaxScore > 0f ? referenceMaxScore : 1f;
        float percentage = Mathf.Clamp01(Mathf.Max(0, scoreFinal) / safeReference);
        int maxBaseYatzis = GetMaxBaseYatzisForIsland(islandId);
        int baseYatzisEarned = Mathf.RoundToInt(percentage * maxBaseYatzis);
        return Mathf.Clamp(baseYatzisEarned, 0, maxBaseYatzis);
    }
}