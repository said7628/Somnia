using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ResultadoNivelUI : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreValue;
    [SerializeField] private TMP_Text bonusValue;
    [SerializeField] private TMP_Text coinsValue;
    [SerializeField] private TMP_Text minScoreValue;
    [SerializeField] private TMP_Text estadoValue;

    private void Start()
    {
        if (scoreValue != null)
        {
            scoreValue.text = TextTypingSession.CurrentScore.ToString();
        }

        if (minScoreValue != null)
        {
            minScoreValue.text = TextTypingSession.MinimumScore.ToString();
        }

        if (estadoValue != null)
        {
            estadoValue.text = TextTypingSession.Passed ? "Completado" : "No completado";
        }

        if (bonusValue != null)
        {
            bonusValue.text = $"+{TextTypingSession.AwardedYatzis}";
        }

        if (coinsValue != null)
        {
            coinsValue.text = TextTypingSession.TotalYatzis.ToString();
        }

        Debug.Log($"[TextTypingResultSuccess] Source level scene={TextTypingSession.LastLevelSceneName} levelId={TextTypingSession.LastLevelId} sourceIslandScene={TextTypingSession.SourceIslandSceneName}");
        Debug.Log($"[TextTypingResultSuccess] Final score={TextTypingSession.CurrentScore}");
        Debug.Log($"[TextTypingResultSuccess] Previous personal best={TextTypingSession.PreviousPersonalBest}");
        Debug.Log($"[TextTypingResultSuccess] New personal best={TextTypingSession.PersonalBest}");
        Debug.Log($"[TextTypingResultSuccess] Yatzis awarded={TextTypingSession.AwardedYatzis} total={TextTypingSession.TotalYatzis} persisted={TextTypingSession.RewardSavedInBackend}");
        Debug.Log($"[TextTypingResultSuccess] Save progress completed. passed={TextTypingSession.Passed}");
        Debug.Log($"[TextTypingResultSuccess] Unlock next level evaluation for levelId={TextTypingSession.LastLevelId}");
    }

    public void IrAMapa()
    {
        string returnScene = TextTypingSession.ResolveReturnScene();
        Debug.Log($"[TextTypingResultSuccess] Map return sourceIslandScene={TextTypingSession.SourceIslandSceneName} sourceLevelScene={TextTypingSession.LastLevelSceneName} returnPosition={TextTypingSession.ReturnPosition} returnRotation={TextTypingSession.ReturnRotation.eulerAngles} sceneLoadedByMapReturn={returnScene}");
        SceneManager.LoadScene(returnScene);
    }
}