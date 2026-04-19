using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;


    [SerializeField] private GameObject configuracionPausa;

    private bool isPaused;

    private void Start()
    {
        pausePanel.SetActive(false);

        if (configuracionPausa
 != null)
            configuracionPausa
    .SetActive(false);

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

        if (configuracionPausa
 != null)
            configuracionPausa
    .SetActive(false);

        Time.timeScale = 0f;
        isPaused = true;
    }

    public void Continuar()
    {
        pausePanel.SetActive(false);


        if (configuracionPausa
 != null)
            configuracionPausa
    .SetActive(false);

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
        // en vez de cambiar de escena, abrimos el panel
        if (configuracionPausa
 != null)
        {
            pausePanel.SetActive(false);
            configuracionPausa
    .SetActive(true);
        }
    }

    //botón para regresar
    public void VolverDesdeConfiguracion()
    {
        if (configuracionPausa
 != null)
        {
            configuracionPausa
    .SetActive(false);
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