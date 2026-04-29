using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class ResultFallidoManager : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button botonMapa;
    [SerializeField] private Button botonReintentar;
    [SerializeField] private Button botonMenu;

    [Header("Scenes")]
    [SerializeField] private string escenaMenu = "Pantalla_principal";

    [Header("UI")]
    [SerializeField] private TMP_Text scoreValue;

    private void Start()
    {
        if (botonMapa != null) botonMapa.onClick.AddListener(IrAMapa);
        if (botonReintentar != null) botonReintentar.onClick.AddListener(ReintentarUltimoNivel);
        if (botonMenu != null) botonMenu.onClick.AddListener(IrAMenu);

        int finalScore = Mathf.Max(0, TextTypingSession.CurrentScore);
        int requiredMinimumScore = Mathf.Max(0, TextTypingSession.MinimumScore);

        if (scoreValue != null)
        {
            scoreValue.text = finalScore.ToString();
        }

        bool passed = finalScore >= requiredMinimumScore;
        TextTypingSession.Passed = passed;

        Debug.Log($"[TextTypingResultFail] Loaded with source scene={TextTypingSession.LastLevelSceneName} levelId={TextTypingSession.LastLevelId} score={finalScore}");
        Debug.Log($"[TextTypingFail] Attempt score: {finalScore}");
        Debug.Log($"[TextTypingFail] Attempt score displayed: {(scoreValue != null ? scoreValue.text : finalScore.ToString())}");
        Debug.Log($"[LevelValidation] score={finalScore} required={requiredMinimumScore} completed={passed}");
    }

    private void IrAMapa()
    {
        string returnScene = TextTypingSession.ResolveReturnScene();
        Debug.Log($"[TextTypingResultFail] Source island scene={TextTypingSession.SourceIslandSceneName} source level scene={TextTypingSession.LastLevelSceneName} return scene={returnScene} return position={TextTypingSession.ReturnPosition}");
        SceneManager.LoadScene(returnScene);
    }

    private void ReintentarUltimoNivel()
    {
        string retryScene = TextTypingSession.LastLevelSceneName;

        if (string.IsNullOrWhiteSpace(retryScene))
        {
            retryScene = !string.IsNullOrWhiteSpace(TextTypingSession.LevelName)
                ? TextTypingSession.LevelName
                : TextTypingSession.ResolveSceneNameFromLevelId(TextTypingSession.LastLevelId > 0 ? TextTypingSession.LastLevelId : TextTypingSession.LevelId);
        }

        if (string.IsNullOrWhiteSpace(retryScene))
        {
            retryScene = "TextTyping";
        }

        Debug.Log($"[TextTypingResultFail] Retry target scene={retryScene} source level scene={TextTypingSession.LastLevelSceneName} levelId={TextTypingSession.LastLevelId}");
        SceneManager.LoadScene(retryScene);
    }

    private void IrAMenu()
    {
        SceneManager.LoadScene(escenaMenu);
    }
}