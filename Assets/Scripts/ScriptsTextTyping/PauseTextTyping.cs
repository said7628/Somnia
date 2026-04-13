using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;

    private bool isPaused;

    private void Start()
    {
        pausePanel.SetActive(false);
        isPaused = false;
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused)
                Continuar();
            else
                AbrirPausa();
        }
    }

    public void AbrirPausa()
    {
        pausePanel.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
    }

    public void Continuar()
    {
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
    }

    public void PruebaBoton()
{
    Debug.Log("si dio click");
}

    public void Reintentar()
    {
        Debug.Log("Reintentar pendiente de implementar.");
    }

    public void IrAMapa()
    {
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene("Mapa");
    }

    public void IrAConfiguracion()
    {
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene("Configuracion");
    }
}