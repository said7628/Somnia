using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuTextTypingUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text scoreMaxText;

    [Header("Escenas")]
    [SerializeField] private string nombreEscenaJugar = "Nuevo juego";
    [SerializeField] private string nombreEscenaMapa = "Mapa";

    [Header("Debug")]
    [SerializeField] private int scoreMaxDebug = 0;

    private int scoreMax;

    private void Start()
    {
        // Por ahora usa un valor local de prueba.
        // Luego esto puede venir de guardado, backend, game manager, etc.
        scoreMax = scoreMaxDebug;
        ActualizarScoreMaxUI();
    }

    private void ActualizarScoreMaxUI()
    {
        if (scoreMaxText != null)
        {
            scoreMaxText.text = scoreMax.ToString();
        }
    }

    // Este metodo queda listo para cuando recibamos el dato desde otro lado.
    public void SetScoreMax(int nuevoScoreMax)
    {
        scoreMax = nuevoScoreMax;
        ActualizarScoreMaxUI();
    }

    public int GetScoreMax()
    {
        return scoreMax;
    }

    public void IrAJugar()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("TextTyping");
    }

    public void IrAMapa()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("escribirescenapapu");
    }
}