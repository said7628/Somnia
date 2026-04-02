using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MenuManager : MonoBehaviour
{
    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // BOTÓN NUEVO JUEGO
        Button botonNuevoJuego = root.Q<Button>("nuevo-juego");
        if (botonNuevoJuego != null)
        {
            botonNuevoJuego.clicked += IrANuevoJuego;
        }
        else
        {
            Debug.LogError("No se encontró el botón 'nuevo-juego'");
        }

        // BOTÓN CONFIGURACIÓN
        Button botonConfig = root.Q<Button>("configuracion");
        if (botonConfig != null)
        {
            botonConfig.clicked += IrAConfiguracion;
        }
        else
        {
            Debug.LogError("No se encontró el botón 'configuracion'");
        }

        // BOTÓN SALIR
        Button botonSalir = root.Q<Button>("salir");
        if (botonSalir != null)
        {
            botonSalir.clicked += SalirDelJuego;
        }
        else
        {
            Debug.LogError("No se encontró el botón 'salir'");
        }
    }

    void IrANuevoJuego()
    {
        SceneManager.LoadScene("Nuevo juego");
    }

    void IrAConfiguracion()
    {
        SceneManager.LoadScene("Configuracion");
    }

    void SalirDelJuego()
    {
        Debug.Log("Saliendo del juego...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}