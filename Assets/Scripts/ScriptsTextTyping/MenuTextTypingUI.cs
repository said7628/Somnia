using TMPro;
using UnityEngine;
using Somnia.UnityClient;
using UnityEngine.SceneManagement;

public class TextTypingMenuRealUI : MonoBehaviour
{
    [SerializeField] private TMP_Text minScoreValue;
    [SerializeField] private TMP_Text maxScoreValue;

    private const string RequiredScoreNameA = "NecessaryScoreValue";
    private const string RequiredScoreNameB = "NecesaryScoreValue";

    private async void Start()
    {
        int minScore = 500;

        if (GameSessionManager.Instance != null && GameSessionManager.Instance.HasSession())
        {
            minScore = GameSessionManager.Instance.GetTextTypingMinimumScore();
        }

        TMP_Text requiredScoreTarget = ResolveRequiredScoreTarget();
        if (requiredScoreTarget != null)
        {
            requiredScoreTarget.text = minScore.ToString();
            Canvas.ForceUpdateCanvases();
            Debug.Log($"[TextTyping] Required score displayed in {requiredScoreTarget.name}: {minScore}");
        }
        else
        {
            Debug.LogError("[TextTyping] ERROR required score value text not found. Expected object named NecessaryScoreValue or NecesaryScoreValue");
        }

        Debug.Log($"[TextTyping] Required score displayed: {minScore}");

        int resolvedLevelId = TextTypingSession.LevelId > 0
            ? TextTypingSession.LevelId
            : TextTypingSession.ResolveLevelIdFromSceneName(SceneManager.GetActiveScene().name, 1);
        int slot = Mathf.Max(1, GameSessionManager.Instance != null ? GameSessionManager.Instance.CurrentSlotNumber : 1);
        int backendBest = await TextTypingProgressService.LoadPersonalBestAsync(resolvedLevelId, slot);

        TextTypingSession.PreviousPersonalBest = backendBest;
        TextTypingSession.PersonalBest = backendBest;

        if (maxScoreValue != null)
            maxScoreValue.text = backendBest.ToString();
    }

    private TMP_Text ResolveRequiredScoreTarget()
    {
        if (minScoreValue != null)
        {
            Debug.Log($"[TextTyping] Required score value target found: {minScoreValue.name}");
            return minScoreValue;
        }

        TMP_Text[] allTextValues = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < allTextValues.Length; i++)
        {
            TMP_Text candidate = allTextValues[i];
            if (candidate == null)
            {
                continue;
            }

            if (candidate.name == RequiredScoreNameA || candidate.name == RequiredScoreNameB)
            {
                minScoreValue = candidate;
                Debug.Log($"[TextTyping] Required score value target found: {candidate.name}");
                return candidate;
            }
        }

        return null;
    }
}