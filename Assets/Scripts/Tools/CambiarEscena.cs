using UnityEngine;
using UnityEngine.SceneManagement;

public class CambiarEscena : MonoBehaviour
{
    public void IrAInventario()
    {
        SceneManager.LoadScene("Inventario");
    }
}