using UnityEngine;
using UnityEngine.SceneManagement;

public class CambiarEscenasBotones : MonoBehaviour
{
    [Header("Nombres de las escenas")]
    [SerializeField] private string escena1;
    [SerializeField] private string escena2;
    [SerializeField] private string escena3;
    [SerializeField] private string escena4;
    [SerializeField] private string escena5;
    [SerializeField] private string Isla1;

    public void IrEscena1()
    {
        SceneManager.LoadScene(escena1);
    }

    public void IrEscena2()
    {
        SceneManager.LoadScene(escena2);
    }

    public void IrEscena3()
    {
        SceneManager.LoadScene(escena3);
    }

    public void IrEscena4()
    {
        SceneManager.LoadScene(escena4);
    }

    public void IrEscena5()
    {
        SceneManager.LoadScene(escena5);
    }

    public void IrIsla()
    {
        SceneManager.LoadScene(Isla1);
    }
}