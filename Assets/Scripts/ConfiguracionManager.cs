using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class ConfiguracionManager : MonoBehaviour
{
    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        Button botonReturn = root.Q<Button>("butreturn");

        if (botonReturn != null)
        {
            botonReturn.clicked += VolverAlMenu;
        }
        else
        {
            Debug.LogError("No se encontro el boton");
        }
    }

    void VolverAlMenu()
    {
        SceneManager.LoadScene("Pantalla_principal");
    }
}