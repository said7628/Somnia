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
        int score = TextTypingSession.CurrentScore;
        int yatzisGanados = TextTypingSession.AwardedYatzis;

        bool perfect = TextTypingSession.Mistakes == 0;
        int perfectBonus = perfect ? (IsIsland2() ? 55 : 50) : 0;

        if (scoreValue != null)
            scoreValue.text = score.ToString();

        // Usa este campo para BONO PERFECTO
        if (yatzisGanadosValue != null)
            yatzisGanadosValue.text = $"+{perfectBonus}";

        // Usa este campo para YATZIS ganados
        if (yatzisTotalesValue != null)
            yatzisTotalesValue.text = yatzisGanados.ToString();

        if (minScoreValue != null)
            minScoreValue.text = TextTypingSession.MinimumScore.ToString();

        if (maxScoreValue != null)
            maxScoreValue.text = TextTypingSession.PersonalBest.ToString();

        if (estadoValue != null)
            estadoValue.text = TextTypingSession.Passed ? "Completado" : "No completado";

        Debug.Log($"[TextTypingWin] Score={score} PerfectBonus=+{perfectBonus} YatzisGanados={yatzisGanados}");
    }

    private bool IsIsland2()
    {
        string source = $"{TextTypingSession.SourceIslandSceneName} {TextTypingSession.LastLevelSceneName}".ToLower();
        return source.Contains("isla2") || source.Contains("island2") || source.Contains("2");
    }

    public void IrAMapa()
    {
        SceneManager.LoadScene(TextTypingSession.ResolveReturnScene());
    }
}