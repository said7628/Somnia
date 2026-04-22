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
    public static int AwardedYatzis = 0;
    public static int TotalYatzis = 0;
    public static bool RewardSavedInBackend = false;

    public static bool Passed = false;
    public static bool WasPlayed = false;

    public static string LastLevelSceneName = "";
    public static int LastLevelId = 0;

    public static string SourceIslandSceneName = "Isla1";
    public static string EntryPointId = "";
    public static Vector3 ReturnPosition = Vector3.zero;
    public static Quaternion ReturnRotation = Quaternion.identity;
    public static bool HasReturnTransform = false;

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
}