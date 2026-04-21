using System;
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

        Debug.Log($"[TextTypingFlow] Source level scene={activeSceneName} levelId={resolvedLevelId}");

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
        TextTypingSession.EdadJugador = playerAge;

        int previousBest = Mathf.Max(0, TextTypingSession.PersonalBest);
        int newBest = Mathf.Max(previousBest, scoreFinal);
        bool progressSaved = false;
        bool completedAfterSave = passed;

        if (saveProgressToBackend)
        {
            int slot = Mathf.Max(1, GameSessionManager.Instance != null ? GameSessionManager.Instance.CurrentSlotNumber : 1);
            TextTypingProgressService.SaveAttemptResult saveResult = await TextTypingProgressService.SaveAttemptAsync(levelId, scoreFinal, passed, slot);

            previousBest = saveResult.PreviousMaxScore;
            newBest = saveResult.NewMaxScore;
            progressSaved = saveResult.Success;
            completedAfterSave = saveResult.CompletedAfterSave;

            Debug.Log($"[TextTypingFlow] Progress save status success={progressSaved} slot={slot} levelId={levelId}");
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
            Debug.LogWarning("[TextTypingFlow] No GameSessionManager found. Using fallback age >=11.");
            return 11;
        }

        string rawBirthDate = session.GetBirthDateRaw();
        if (session.TryGetBirthDate(out DateTime birthDate))
        {
            int age = session.GetPlayerAge();
            Debug.Log($"[TextTypingFlow] Birth date from DB/session={birthDate:yyyy-MM-dd}. Calculated age={age}");
            return age;
        }

        Debug.LogWarning($"[TextTypingFlow] Birth date unavailable or invalid. rawBirthDate='{rawBirthDate}'. Using fallback age >=11.");
        return 11;
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