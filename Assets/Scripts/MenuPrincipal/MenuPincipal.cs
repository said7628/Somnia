using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MenuManager : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void SomniaExitToMainSite();
#endif

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;


        Button botonNuevoJuego = root.Q<Button>("nuevo-juego");
        if (botonNuevoJuego != null)
        {
            botonNuevoJuego.clicked += IrANuevoJuego;
        }
        else
        {
            Debug.LogError("No se ");
        }


        Button botonConfig = root.Q<Button>("configuracion");
        if (botonConfig != null)
        {
            botonConfig.clicked += IrAConfiguracion;
        }
        else
        {
            Debug.LogError("No se ");
        }

        Button botonContinuar = root.Q<Button>("continuar");
        if (botonContinuar != null)
        {
            botonContinuar.clicked += IrAContinuar;
        }
        else
        {
            Debug.LogError("No se ");
        }

        Button botonSalir = root.Q<Button>("salir");
        if (botonSalir != null)
        {
            botonSalir.clicked += SalirDelJuego;
        }
        else
        {
            Debug.LogError("No se ");
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

    void IrAContinuar()
    {
        SceneManager.LoadScene("ContinuarPartida");
    }

    void SalirDelJuego()
    {
        Debug.Log("Saliendo del juego...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
        SomniaExitToMainSite();
#else
        Application.Quit();
#endif
    }
}