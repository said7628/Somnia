using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuBotonesCanvas : MonoBehaviour
{
    [Header("Botones")]
    public Button jugarButton;
    public Button mapaButton;

    [Header("Escenas")]
    public string escenaJuego = "Textyping";
    public string escenaMapa = "Isla1";

    void Start()
    {
        jugarButton.onClick.AddListener(IrAJuego);
        mapaButton.onClick.AddListener(IrAMapa);
    }

    void IrAJuego()
    {
        SceneManager.LoadScene(escenaJuego);
    }

    void IrAMapa()
    {
        SceneManager.LoadScene(escenaMapa);
    }
}