using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ResultadoNivelUITextTyping : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreValue;
    [SerializeField] private TMP_Text minScoreValue;
    [SerializeField] private TMP_Text maxScoreValue;
    [SerializeField] private TMP_Text estadoValue;
    [SerializeField] private TMP_Text yatzisGanadosValue;
    [SerializeField] private TMP_Text yatzisTotalesValue;

    private void Start()
    {
        if (scoreValue != null)
            scoreValue.text = TextTypingSession.CurrentScore.ToString();

        if (minScoreValue != null)
            minScoreValue.text = TextTypingSession.MinimumScore.ToString();

        if (maxScoreValue != null)
            maxScoreValue.text = TextTypingSession.PersonalBest.ToString();

        if (estadoValue != null)
            estadoValue.text = TextTypingSession.Passed ? "Completado" : "No completado";

        if (yatzisGanadosValue != null)
            yatzisGanadosValue.text = $"+{TextTypingSession.AwardedYatzis}";


        if (yatzisTotalesValue != null)
            yatzisTotalesValue.text = TextTypingSession.AwardedYatzis.ToString();

        Debug.Log($"[TextTypingResultUI] Source level scene={TextTypingSession.LastLevelSceneName} levelId={TextTypingSession.LastLevelId} sourceIslandScene={TextTypingSession.SourceIslandSceneName}");
        Debug.Log($"[TextTypingResultUI] Final score={TextTypingSession.CurrentScore}");
        Debug.Log($"[TextTypingResultUI] Previous personal best={TextTypingSession.PreviousPersonalBest}");
        Debug.Log($"[TextTypingResultUI] New personal best={TextTypingSession.PersonalBest}");
        Debug.Log($"[TextTypingResultUI] Yatzis awarded(current run)={TextTypingSession.AwardedYatzis} total(account)={TextTypingSession.TotalYatzis} persisted={TextTypingSession.RewardSavedInBackend}");
    }

    public void IrAMapa()
    {
        string returnScene = TextTypingSession.ResolveReturnScene();
        if (TextTypingSession.Passed)
        {
            GameSceneTransitionContext.PlayLevelPassedAudioOnNextIslandLoad = true;
            GameSceneTransitionContext.PassedLevelId = TextTypingSession.LastLevelId;
        }
        else
        {
            GameSceneTransitionContext.PlayLevelPassedAudioOnNextIslandLoad = false;
            GameSceneTransitionContext.PassedLevelId = -1;
            Debug.Log("[IslandLevelManager] Failed flow detected: no unlock, no passed audio");
        }
        Debug.Log($"[TextTypingResultUI] Map return sourceIslandScene={TextTypingSession.SourceIslandSceneName} sourceLevelScene={TextTypingSession.LastLevelSceneName} returnPosition={TextTypingSession.ReturnPosition} returnRotation={TextTypingSession.ReturnRotation.eulerAngles} sceneLoadedByMapReturn={returnScene}");
        SceneManager.LoadScene(returnScene);
    }
}