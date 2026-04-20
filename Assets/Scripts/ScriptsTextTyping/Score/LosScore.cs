using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ResultFallidoManager : MonoBehaviour
{
    [Header("Botones")]
    public Button botonMapa;
    public Button botonReintentar;
    public Button botonMenu;

    [Header("Escenas")]
    public string escenaMapa = "Isla1";          // ponemos el nombre escena de mapa
    public string escenaMenu = "Pantalla_principal"; // ponemos tu menu principal

    void Start()
    {
        botonMapa.onClick.AddListener(IrAMapa);
        botonReintentar.onClick.AddListener(Reintentar);
        botonMenu.onClick.AddListener(IrAMenu);
    }

    void IrAMapa()
    {
        SceneManager.LoadScene(escenaMapa);
    }

    void Reintentar()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void IrAMenu()
    {
        SceneManager.LoadScene(escenaMenu);
    }
}