using UnityEngine;
using UnityEngine.UIElements;

public class SistemaTienda : MonoBehaviour
{
    private Label etiquetaMonedas; // Para mostrar tus 500 monedas
    
    [Header("Datos del Jugador")]
    public int monedasJugador = 500; 

    void Start()
    {
        // 1. Buscamos el nombre exacto que pusiste en el PanelTienda
        var root = GetComponent<UIDocument>().rootVisualElement;
        etiquetaMonedas = root.Q<Label>("EtiquetaMonedas"); 
        ActualizarTextoMonedas();
    }

    // --- FUNCIÓN PÚBLICA PARA COMPRAR ---
    // Ahora el Controlador la llamará cada vez que alguien haga clic en un estandarte
    public void IntentarCompra(int precioItem, Button botonPresionado, VisualElement bannerSoldOut)
    {
        Debug.Log("¿Encontré el banner?: " + (bannerSoldOut != null));
        if (monedasJugador >= precioItem)
        {
            monedasJugador -= precioItem;
            ActualizarTextoMonedas(); // Actualizamos el visual de las monedas

            // Mostramos el letrero de Sold Out si existe
            if (bannerSoldOut != null)
                bannerSoldOut.style.display = DisplayStyle.Flex;

            // Desactivamos el botón para evitar compras dobles
            botonPresionado.SetEnabled(false);
            Debug.Log("¡Compra exitosa! Te quedan: " + monedasJugador);
        }
        else
        {
            Debug.LogWarning("No tienes lana bro. Tienes: " + monedasJugador);
        }
    }

    void ActualizarTextoMonedas()
    {
        if (etiquetaMonedas != null)
        {
            etiquetaMonedas.text = monedasJugador.ToString();
        }
    }
}