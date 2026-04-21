using TMPro;
using UnityEngine;
using Somnia.UnityClient;
using UnityEngine.SceneManagement;

public class TextTypingMenuRealUI : MonoBehaviour
{
    [SerializeField] private TMP_Text minScoreValue;
    [SerializeField] private TMP_Text maxScoreValue;

    private async void Start()
    {
        int minScore = 500;

        if (GameSessionManager.Instance != null && GameSessionManager.Instance.HasSession())
        {
            minScore = GameSessionManager.Instance.GetTextTypingMinimumScore();
        }

        if (minScoreValue != null)
            minScoreValue.text = minScore.ToString();

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
}