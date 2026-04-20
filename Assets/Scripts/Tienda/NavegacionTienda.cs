using UnityEngine;
using UnityEngine.UIElements; 
using UnityEngine.SceneManagement; 

public class NavegacionTienda : MonoBehaviour
{
    private UIDocument documentoUI;
    private Button botonRegresar;

    void OnEnable()
    {
        documentoUI = GetComponent<UIDocument>();

        botonRegresar = documentoUI.rootVisualElement.Q<Button>("BotonRegresa");

        if (botonRegresar != null)
        {
            botonRegresar.clicked += RegresarAIsla;
        }
        else
        {
            Debug.LogError("¡ERROR, no se encontro ningun boton ");
        }
    }

private void RegresarAIsla()
    {
        // 1. Le preguntamos a la memoria a dónde tenemos que ir
        string destino = Memoria_Islas.islaDeDondeVengo;

        Debug.Log("¡Viajando de regreso a: " + destino + "!");
        
        // 2. Le decimos a Unity que cargue esa escena
        SceneManager.LoadScene(destino); 
    }

    void OnDisable()
    {
        if (botonRegresar != null)
        {
            botonRegresar.clicked -= RegresarAIsla;
        }
    }
}