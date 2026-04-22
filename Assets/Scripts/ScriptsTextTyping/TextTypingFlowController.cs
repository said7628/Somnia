using System.Threading.Tasks;
using Somnia.UnityClient;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TextTypingFlowController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private Timer timerScript;

    [Header("Scenes")]
    [SerializeField] private string successScene = "ScoreTextyping";
    [SerializeField] private string failScene = "ScoreTextypingFallido";

    [Header("Progress")]
    [SerializeField] private bool saveProgressToBackend = true;
    [SerializeField] private int fallbackDbLevelId = 1;

    private bool isFinishing;

    private async void Start()
    {
        if (scoreManager == null)
        {
            scoreManager = FindFirstObjectByType<ScoreManager>();
        }

        if (timerScript == null)
        {
            timerScript = FindFirstObjectByType<Timer>();
        }

        string activeSceneName = SceneManager.GetActiveScene().name;
        int resolvedLevelId = TextTypingSession.LevelId > 0
            ? TextTypingSession.LevelId
            : TextTypingSession.ResolveLevelIdFromSceneName(activeSceneName, fallbackDbLevelId);

        TextTypingSession.TrackLevelContext(resolvedLevelId, activeSceneName);

        int startSlot = Mathf.Max(1, GameSessionManager.Instance != null ? GameSessionManager.Instance.CurrentSlotNumber : 1);
        Debug.Log($"[TextTypingFlow] Enter level -> slot={startSlot} levelId={resolvedLevelId} scene={activeSceneName}");

        await LoadPersonalBestForHudAsync(resolvedLevelId);
    }

    private void Update()
    {
        if (isFinishing || timerScript == null)
        {
            return;
        }

        if (timerScript.GetTime() <= 0f)
        {
            Debug.Log("[TextTypingFlow] Time expired detected. Starting final flow.");
            _ = FinalizarNivelAsync();
        }
    }

    public void FinalizarNivel()
    {
        _ = FinalizarNivelAsync();
    }

    public async Task FinalizarNivelAsync()
    {
        if (isFinishing)
        {
            return;
        }

        isFinishing = true;

        ResolveReferencesIfNeeded();

        int scoreFinal = scoreManager != null ? scoreManager.GetScore() : 0;
        int levelId = TextTypingSession.LevelId > 0
            ? TextTypingSession.LevelId
            : TextTypingSession.ResolveLevelIdFromSceneName(SceneManager.GetActiveScene().name, fallbackDbLevelId);
        string levelSceneName = string.IsNullOrWhiteSpace(TextTypingSession.LevelName)
            ? SceneManager.GetActiveScene().name
            : TextTypingSession.LevelName;

        TextTypingSession.TrackLevelContext(levelId, levelSceneName);

        Debug.Log($"[TextTypingFlow] Final score for level scene={levelSceneName} levelId={levelId} score={scoreFinal}");

        int playerAge = ResolvePlayerAge();
        int minimumScore = TextTypingPlayerRules.ObtenerPuntajeMinimoPorEdad(playerAge);
        bool passed = scoreFinal >= minimumScore;

        Debug.Log($"[TextTypingFlow] Final score={scoreFinal}");
        Debug.Log($"[TextTypingFlow] Minimum required score={minimumScore}");
        Debug.Log($"[TextTypingFlow] Pass/fail result={(passed ? "PASS" : "FAIL")}");

        TextTypingSession.CurrentScore = scoreFinal;
        TextTypingSession.MinimumScore = minimumScore;
        TextTypingSession.Passed = passed;
        TextTypingSession.WasPlayed = true;
        TextTypingSession.PlayerAge = playerAge;
        TextTypingSession.CalculatedYatzis = TextTypingYatzisCalculator.CalcularComponenteExtraRedondeada(scoreFinal, playerAge);
        TextTypingSession.AwardedYatzis = 0;
        TextTypingSession.TotalYatzis = 0;
        TextTypingSession.RewardSavedInBackend = false;

        int previousBest = Mathf.Max(0, TextTypingSession.PersonalBest);
        int newBest = Mathf.Max(previousBest, scoreFinal);
        bool completedAfterSave = passed;

        if (saveProgressToBackend)
        {
            int slot = Mathf.Max(1, GameSessionManager.Instance != null ? GameSessionManager.Instance.CurrentSlotNumber : 1);
            TextTypingProgressService.SaveAttemptResult saveResult = await TextTypingProgressService.SaveAttemptAsync(levelId, scoreFinal, passed, slot);
            TextTypingProgressService.ScoreRewardResult scoreRewardResult = await TextTypingProgressService.SaveScoreAndRewardAsync(levelId, scoreFinal);

            previousBest = saveResult.PreviousMaxScore;
            newBest = saveResult.NewMaxScore;
            completedAfterSave = saveResult.CompletedAfterSave;

            TextTypingSession.AwardedYatzis = scoreRewardResult.AwardedYatzis;
            TextTypingSession.TotalYatzis = scoreRewardResult.TotalYatzis;
            TextTypingSession.RewardSavedInBackend = scoreRewardResult.Success;

            Debug.Log($"[TextTypingFlow] Progress save status success={saveResult.Success} slot={slot} levelId={levelId}");
            Debug.Log($"[TextTypingFlow] Reward save status success={scoreRewardResult.Success} awarded={scoreRewardResult.AwardedYatzis} total={scoreRewardResult.TotalYatzis} backendMultiplier={scoreRewardResult.BackendMultiplier}");

            if (!scoreRewardResult.Success)
            {
                Debug.LogWarning($"[TextTypingFlow] Could not persist yatzis reward in backend. reason={scoreRewardResult.Message}");
            }
        }
        else
        {
            Debug.Log("[TextTypingFlow] Progress save skipped because saveProgressToBackend is disabled.");
        }

        TextTypingSession.PreviousPersonalBest = previousBest;
        TextTypingSession.PersonalBest = newBest;

        Debug.Log($"[TextTypingFlow] Previous personal best={previousBest}");
        Debug.Log($"[TextTypingFlow] New personal best={newBest}");
        Debug.Log($"[TextTypingFlow] Unlock next level check completedLevel={completedAfterSave} levelId={levelId}");

        string targetScene = passed ? successScene : failScene;
        Debug.Log($"[TextTypingFlow] Source level scene={levelSceneName}");
        Debug.Log($"[TextTypingFlow] Source island scene={TextTypingSession.SourceIslandSceneName}");
        Debug.Log($"[TextTypingFlow] Target scene={targetScene}");
        SceneManager.LoadScene(targetScene);
    }


    private void ResolveReferencesIfNeeded()
    {
        if (scoreManager == null)
        {
            scoreManager = FindFirstObjectByType<ScoreManager>();
        }

        if (timerScript == null)
        {
            timerScript = FindFirstObjectByType<Timer>();
        }
    }

    private int ResolvePlayerAge()
    {
        GameSessionManager session = GameSessionManager.Instance;

        if (session == null)
        {
            Debug.LogWarning("[TextTypingFlow] No GameSessionManager found. Using fallback age >11.");
            return 12;
        }

        int backendAge = session.PlayerAge > 0 ? session.PlayerAge : TextTypingSession.PlayerAge;
        if (backendAge > 0)
        {
            Debug.Log($"[TextTypingFlow] Age from backend/session={backendAge}");
            return backendAge;
        }

        Debug.LogWarning("[TextTypingFlow] backend age unavailable. Using fallback age >11.");
        return 12;
    }

    private async Task LoadPersonalBestForHudAsync(int levelId)
    {
        int slot = Mathf.Max(1, GameSessionManager.Instance != null ? GameSessionManager.Instance.CurrentSlotNumber : 1);
        int best = await TextTypingProgressService.LoadPersonalBestAsync(levelId, slot);

        TextTypingSession.PreviousPersonalBest = best;
        TextTypingSession.PersonalBest = best;

        Debug.Log($"[TextTypingFlow] Loaded personal best for HUD scene={TextTypingSession.LevelName} levelId={levelId} slot={slot} best={best}");
    }
}