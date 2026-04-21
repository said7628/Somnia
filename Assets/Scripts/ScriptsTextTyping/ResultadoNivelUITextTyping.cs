using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ResultadoNivelUITextTyping : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreValue;
    [SerializeField] private TMP_Text minScoreValue;
    [SerializeField] private TMP_Text maxScoreValue;
    [SerializeField] private TMP_Text estadoValue;


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

        Debug.Log($"[TextTypingResultUI] Source level scene={TextTypingSession.LastLevelSceneName} levelId={TextTypingSession.LastLevelId} sourceIslandScene={TextTypingSession.SourceIslandSceneName}");
        Debug.Log($"[TextTypingResultUI] Final score={TextTypingSession.CurrentScore}");
        Debug.Log($"[TextTypingResultUI] Previous personal best={TextTypingSession.PreviousPersonalBest}");
        Debug.Log($"[TextTypingResultUI] New personal best={TextTypingSession.PersonalBest}");
    }

    public void IrAMapa()
    {
        string returnScene = TextTypingSession.ResolveReturnScene();
        Debug.Log($"[TextTypingResultUI] Map return sourceIslandScene={TextTypingSession.SourceIslandSceneName} sourceLevelScene={TextTypingSession.LastLevelSceneName} returnPosition={TextTypingSession.ReturnPosition} returnRotation={TextTypingSession.ReturnRotation.eulerAngles} sceneLoadedByMapReturn={returnScene}");
        SceneManager.LoadScene(returnScene);
    }
}