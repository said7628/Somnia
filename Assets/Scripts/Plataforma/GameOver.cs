using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOver : MonoBehaviour
{
    [Header("Escenas")]
    [SerializeField] private string escenaMenu = "Pantalla_principal";

    private void Start()
    {
        Time.timeScale = 1f;
    }

    public void Reintentar()
    {
        string escenaActual = SceneManager.GetActiveScene().name;

        Time.timeScale = 1f;

        SceneManager.LoadScene(escenaActual);
    }

    public void IrAMapa()
    {
        string returnScene = TextTypingSession.ResolveReturnScene();

        Time.timeScale = 1f;

        SceneManager.LoadScene(returnScene);
    }

    public void IrAMenu()
    {
        Time.timeScale = 1f;

        Debug.Log("[GameOver] Volviendo al menú principal: " + escenaMenu);
        SceneManager.LoadScene(escenaMenu);
    }
}