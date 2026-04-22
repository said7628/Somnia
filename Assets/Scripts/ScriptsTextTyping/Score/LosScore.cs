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

        int playerAge = TextTypingSession.EdadJugador > 0 ? TextTypingSession.EdadJugador : 11;
        int requiredMinimumScore = TextTypingPlayerRules.ObtenerPuntajeMinimoPorEdad(playerAge);
        TextTypingSession.MinimumScore = requiredMinimumScore;

        int finalScore = Mathf.Max(0, TextTypingSession.CurrentScore);

        if (scoreValue != null)
        {
            scoreValue.text = finalScore.ToString();
        }

        bool passed = TextTypingPlayerRules.PasoNivel(playerAge, finalScore);
        TextTypingSession.Passed = passed;

        Debug.Log($"[TextTypingResultFail] Loaded with source scene={TextTypingSession.LastLevelSceneName} levelId={TextTypingSession.LastLevelId} score={finalScore}");
        Debug.Log($"[TextTypingResultFail] player age={playerAge}");
        Debug.Log($"[TextTypingResultFail] required minimum score for fail screen={requiredMinimumScore}");
        Debug.Log($"[TextTypingResultFail] pass/fail recomputed from age+score={(passed ? "PASS" : "FAIL")}");
        Debug.Log($"[TextTypingResultFail] fail screen displayed score={(scoreValue != null ? scoreValue.text : finalScore.ToString())}");
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