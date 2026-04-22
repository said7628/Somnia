using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuCanvas : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject configuracionPausa;

    [Header("Escenas")]
    [SerializeField] private string escenaMapa = "Mapa";
    [SerializeField] private string escenaMenu = "Pantalla_principal";

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
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                if (configuracionPausa != null && configuracionPausa.activeSelf)
                {
                    VolverDesdeConfiguracion();
                }
                else
                {
                    Continuar();
                }
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
        string escenaActual = SceneManager.GetActiveScene().name;

        Time.timeScale = 1f;
        isPaused = false;

        SceneManager.LoadScene(escenaActual);
    }

    public void IrAMapa()
    {
        Time.timeScale = 1f;
        isPaused = false;

        SceneManager.LoadScene(escenaMapa);
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

        SceneManager.LoadScene(escenaMenu);
    }
}