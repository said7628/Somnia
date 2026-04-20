using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ResultadoNivelUITextTyping : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreValue;
    [SerializeField] private TMP_Text minScoreValue;
    [SerializeField] private TMP_Text maxScoreValue;
    [SerializeField] private TMP_Text estadoValue;

    [SerializeField] private string mapaScene = "Mapa";

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
    }

    public void IrAMapa()
    {
        SceneManager.LoadScene(mapaScene);
    }
}