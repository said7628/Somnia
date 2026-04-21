using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [Header("Botones")]
    public Button botonReintentar;
    public Button botonMenu;
    public Button botonMapa;

    [Header("Escenas")]
    public string escenaMenu = "MenuPrincipal";
    public string escenaMapa = "Mapa";

    void Start()
    {
        botonReintentar.onClick.AddListener(Reintentar);
        botonMenu.onClick.AddListener(IrAMenu);
        botonMapa.onClick.AddListener(IrAMapa);
    }

    void Reintentar()
    {
        // Pendiente de implementar
        Debug.Log("Reintentar aún no implementado");
    }

    void IrAMenu()
    {
        SceneManager.LoadScene(escenaMenu);
    }

    void IrAMapa()
    {
        SceneManager.LoadScene(escenaMapa);
    }
}