using UnityEngine;
using UnityEngine.UIElements;

public class ControladorTienda : MonoBehaviour
{
    [Header("Datos de la Tienda Actual")]
    public DatosTiendaIsla islaActual;

    [Header("El Molde del Estandarte")]
    public VisualTreeAsset plantillaItemTienda;

    [Header("Imágenes Estado NORMAL")]
    public Sprite imgOjosNormal;
    public Sprite imgColorNormal;
    public Sprite imgRopaNormal;

    [Header("Imágenes Estado ACTIVO")]
    public Sprite imgOjosActivo;
    public Sprite imgColorActivo;
    public Sprite imgRopaActivo;

    private UIDocument tiendaUI;
    
    private Button btnOjos;
    private Button btnColor;
    private Button btnRopa;
    private VisualElement contenedorOpciones;

    void OnEnable()
    {
        tiendaUI = GetComponent<UIDocument>();
        var root = tiendaUI.rootVisualElement;

        btnOjos = root.Q<Button>("BtnOjos");
        btnColor = root.Q<Button>("BtnColor");
        btnRopa = root.Q<Button>("BtnRopa");
        contenedorOpciones = root.Q<VisualElement>("ContenedorOpciones");

        btnOjos.RegisterCallback<ClickEvent>(MostrarOpcionesOjos);
        btnColor.RegisterCallback<ClickEvent>(MostrarOpcionesColor);
        btnRopa.RegisterCallback<ClickEvent>(MostrarOpcionesRopa);

        ResetearBotones();

        if (islaActual != null)
        {
            CargarDatosIsla(islaActual);
        }
    }

    public void CargarDatosIsla(DatosTiendaIsla datos)
    {
        Debug.Log("¡Bienvenido a la tienda de: " + datos.nombreIsla + "!");
    }

    private void ResetearBotones()
    {
        if (imgOjosNormal != null) btnOjos.style.backgroundImage = new StyleBackground(imgOjosNormal);
        if (imgColorNormal != null) btnColor.style.backgroundImage = new StyleBackground(imgColorNormal);
        if (imgRopaNormal != null) btnRopa.style.backgroundImage = new StyleBackground(imgRopaNormal);
    }

    private void MostrarOpcionesOjos(ClickEvent evt)
    {
        ResetearBotones();
        if (imgOjosActivo != null) btnOjos.style.backgroundImage = new StyleBackground(imgOjosActivo);

        contenedorOpciones.Clear();
        
        if (islaActual.opcionesOjos != null)
        {
            foreach (Sprite miSprite in islaActual.opcionesOjos)
            {
                CrearPosterDinamico(miSprite, "150");
            }
        }
    }

    private void MostrarOpcionesColor(ClickEvent evt)
    {
        ResetearBotones();
        if (imgColorActivo != null) btnColor.style.backgroundImage = new StyleBackground(imgColorActivo);
        
        contenedorOpciones.Clear();
        
        if (islaActual.opcionesColor != null)
        {
            foreach (Sprite miSprite in islaActual.opcionesColor)
            {
                CrearPosterDinamico(miSprite, "200"); 
            }
        }
    }

    private void MostrarOpcionesRopa(ClickEvent evt)
    {
        ResetearBotones();
        if (imgRopaActivo != null) btnRopa.style.backgroundImage = new StyleBackground(imgRopaActivo);
        
        contenedorOpciones.Clear();
        
        if (islaActual.opcionesRopa != null)
        {
            foreach (Sprite miSprite in islaActual.opcionesRopa)
            {
                CrearPosterDinamico(miSprite, "500"); 
            }
        }
    }

    private void CrearPosterDinamico(Sprite imagenEstandarte, string precioTexto)
    {
        TemplateContainer instancia = plantillaItemTienda.Instantiate();

        VisualElement fondo = instancia.Q<VisualElement>("FondoEstandarte");
        Label etiquetaPrecio = instancia.Q<Label>("Precio");
        Button btnCompra = instancia.Q<Button>("BtnCompra");

        if (fondo != null && imagenEstandarte != null)
        {
            fondo.style.backgroundImage = new StyleBackground(imagenEstandarte);
        }
        
        if (etiquetaPrecio != null)
        {
            etiquetaPrecio.text = precioTexto;
        }

        instancia.style.marginRight = 15;
        instancia.style.marginLeft = 15;

        contenedorOpciones.Add(instancia);
    }

    void OnDisable()
    {
        btnOjos.UnregisterCallback<ClickEvent>(MostrarOpcionesOjos);
        btnColor.UnregisterCallback<ClickEvent>(MostrarOpcionesColor);
        btnRopa.UnregisterCallback<ClickEvent>(MostrarOpcionesRopa);
    }
}