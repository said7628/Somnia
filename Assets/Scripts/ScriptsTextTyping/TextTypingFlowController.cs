using UnityEngine;
using UnityEngine.SceneManagement;
using Somnia.UnityClient;

public class TextTypingFlowController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private Timer timerScript;

    [Header("Escenas")]
    [SerializeField] private string successScene = "ScoreTextyping";
    [SerializeField] private string failScene = "ScoreTextypingFallido";

    private bool finalizado = false;
    private int minimumScore = 500;

    private void Start()
    {
        if (GameSessionManager.Instance != null && GameSessionManager.Instance.HasSession())
        {
            minimumScore = GameSessionManager.Instance.GetTextTypingMinimumScore();
        }

        TextTypingSession.MinimumScore = minimumScore;
    }

    private void Update()
    {
        if (finalizado) return;
        if (timerScript == null) return;

        if (timerScript.GetTime() <= 0)
        {
            FinalizarNivel();
        }
    }

    public void FinalizarNivel()
    {
        if (finalizado) return;
        finalizado = true;

        int scoreFinal = 0;
        if (scoreManager != null)
            scoreFinal = scoreManager.GetScore();

        bool paso = scoreFinal >= minimumScore;

        TextTypingSession.CurrentScore = scoreFinal;
        TextTypingSession.MinimumScore = minimumScore;
        TextTypingSession.Passed = paso;
        TextTypingSession.WasPlayed = true;

        if (scoreFinal > TextTypingSession.PersonalBest)
            TextTypingSession.PersonalBest = scoreFinal;

        SceneManager.LoadScene(paso ? successScene : failScene);
    }
}