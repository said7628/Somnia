using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class NuevoJuegoManager : MonoBehaviour
{
    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        Button botonRegreso = root.Q<Button>("Regreso");
        if (botonRegreso != null)
        {
            botonRegreso.clicked += IrAMenuPrincipal;
        }
        else
        {
            Debug.LogError("No se encontró el botón 'Regreso'");
        }

        // GUARDADOS
        Button g1 = root.Q<Button>("Guardado1");
        Button g2 = root.Q<Button>("Guardado2");
        Button g3 = root.Q<Button>("Guardado3");

        if (g1 != null) g1.clicked += IrAMapa;
        else Debug.LogError("No se encontró 'Guardado1'");

        if (g2 != null) g2.clicked += IrAMapa;
        else Debug.LogError("No se encontró 'Guardado2'");

        if (g3 != null) g3.clicked += IrAMapa;
        else Debug.LogError("No se encontró 'Guardado3'");
    }

    void IrAMenuPrincipal()
    {
        SceneManager.LoadScene("Pantalla principal");
    }

    void IrAMapa()
    {
        SceneManager.LoadScene("Mapa");
    }
}