using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class Navegacion_Isla1 : MonoBehaviour
{
    private UIDocument documentoUI;
    
    // Botones de la Isla 1
    private Button botonTienda;
    private Button botonInventario;
    private Button botonMenu;
    private Button botonNivel1;
    private Button botonNivel2;
    private Button botonNivel3;

    // Cantidad de monedas
    private Label textoMonedas;

    void OnEnable()
    {
        // 1. Obtenemos el documento de la UI que está pegado a este GameObject
        documentoUI = GetComponent<UIDocument>();

        // 2. Buscamos cada elemento por el nombre (ID) que le pusimos en el UI Builder
        var root = documentoUI.rootVisualElement;

        botonTienda = root.Q<Button>("BotonTienda");
        botonInventario = root.Q<Button>("BotonInventario");
        botonMenu = root.Q<Button>("BotonMenu");
        
        botonNivel1 = root.Q<Button>("Nivel1");
        botonNivel2 = root.Q<Button>("Nivel2");
        botonNivel3 = root.Q<Button>("Nivel3");

        textoMonedas = root.Q<Label>("CantidadMonedas");

        // 3. Suscribimos las funciones a los eventos de clic
        if (botonTienda != null) botonTienda.clicked += IrATienda;
        if (botonInventario != null) botonInventario.clicked += IrAInventario;
        if (botonMenu != null) botonMenu.clicked += IrAMenu;

        if (botonNivel1 != null) botonNivel1.clicked += IrANivel1;
        if (botonNivel2 != null) botonNivel2.clicked += IrANivel2;
        if (botonNivel3 != null) botonNivel3.clicked += IrANivel3;

        // 4. Actualizamos el contador de monedas nada más empezar
        ActualizarInterfaz();
    }

    private void ActualizarInterfaz()
    {
        if (textoMonedas != null)
        {
            // Tomamos el valor de la memoria estática y lo ponemos en el Label
            textoMonedas.text = Memoria_Islas.misMonedas.ToString();
        }
    }

    // Funciones de navegación para cada botón

    private void IrATienda()
    {
        // Antes de irnos, guardamos de dónde venimos en la memoria
        Memoria_Islas.islaDeDondeVengo = SceneManager.GetActiveScene().name; 
        SceneManager.LoadScene("Tienda"); 
    }

    private void IrAInventario()
    {
        SceneManager.LoadScene("Inventario"); 
    }

    private void IrAMenu()
    {
        SceneManager.LoadScene("Pantalla_principal"); 
    }

    private void IrANivel1() { SceneManager.LoadScene("Nivel1"); }
    private void IrANivel2() { SceneManager.LoadScene("Nivel2"); }
    private void IrANivel3() { SceneManager.LoadScene("Nivel3"); }

    // Limpieza de eventos para evitar errores de memoria
    void OnDisable()
    {
        if (botonTienda != null) botonTienda.clicked -= IrATienda;
        if (botonInventario != null) botonInventario.clicked -= IrAInventario;
        if (botonMenu != null) botonMenu.clicked -= IrAMenu;
        if (botonNivel1 != null) botonNivel1.clicked -= IrANivel1;
        if (botonNivel2 != null) botonNivel2.clicked -= IrANivel2;
        if (botonNivel3 != null) botonNivel3.clicked -= IrANivel3;
    }
}