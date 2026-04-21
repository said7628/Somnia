using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject configuracionPausa;

    private bool isPaused;

    private void Start()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (configuracionPausa != null)
        {
            configuracionPausa.SetActive(false);
        }

        isPaused = false;
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused)
            {
                Continuar();
            }
            else
            {
                AbrirPausa();
            }
        }
    }

    public void AbrirPausa()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }

        if (configuracionPausa != null)
        {
            configuracionPausa.SetActive(false);
        }

        Time.timeScale = 0f;
        isPaused = true;
    }

    public void Continuar()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (configuracionPausa != null)
        {
            configuracionPausa.SetActive(false);
        }

        Time.timeScale = 1f;
        isPaused = false;
    }

    public void Reintentar()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        Time.timeScale = 1f;
        isPaused = false;

        Debug.Log($"[TextTypingPause] Retry button loading scene={currentScene}");
        SceneManager.LoadScene(currentScene);
    }

    public void IrAMapa()
    {
        string returnScene = TextTypingSession.ResolveReturnScene();

        Time.timeScale = 1f;
        isPaused = false;

        Debug.Log(
            $"[TextTypingPause] Map return sourceIslandScene={TextTypingSession.SourceIslandSceneName} " +
            $"sourceLevelScene={SceneManager.GetActiveScene().name} returnPosition={TextTypingSession.ReturnPosition} " +
            $"returnRotation={TextTypingSession.ReturnRotation.eulerAngles} sceneLoadedByMapReturn={returnScene}"
        );

        SceneManager.LoadScene(returnScene);
    }

    public void IrAConfiguracion()
    {
        if (configuracionPausa != null)
        {
            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }

            configuracionPausa.SetActive(true);
        }
    }

    public void VolverDesdeConfiguracion()
    {
        if (configuracionPausa != null)
        {
            configuracionPausa.SetActive(false);
        }

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }
    }

    public void IrAMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene("Pantalla_principal");
    }
}