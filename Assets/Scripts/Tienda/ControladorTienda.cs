using UnityEngine;
using UnityEngine.UIElements;

public class ControladorTienda : MonoBehaviour
{
    // --- DATOS PRINCIPALES ---
    [Header("Datos de la Tienda Actual")]
    public DatosTiendaIsla islaActual;

    [Header("El Molde del Estandarte")]
    public VisualTreeAsset plantillaItemTienda;

    // --- IMÁGENES DE LOS BOTONES ---
    [Header("Imágenes Estado NORMAL")]
    public Sprite imgOjosNormal;
    public Sprite imgColorNormal;
    public Sprite imgRopaNormal;

    [Header("Imágenes Estado ACTIVO")]
    public Sprite imgOjosActivo;
    public Sprite imgColorActivo;
    public Sprite imgRopaActivo;

    // --- VARIABLES PRIVADAS (El cerebro del UI) ---
    private UIDocument tiendaUI;
    private Button btnOjos;
    private Button btnColor;
    private Button btnRopa;
    private VisualElement contenedorOpciones;
    
    // Referencia al otro script para poder comprar
    private SistemaTienda sistemaCompra;

    void OnEnable()
    {
        // 1. Conectamos con el documento UI de tu escena
        tiendaUI = GetComponent<UIDocument>();
        sistemaCompra = GetComponent<SistemaTienda>(); // Buscamos el banco en el mismo objeto
        var root = tiendaUI.rootVisualElement;

        // 2. Buscamos los botones usando el nombre EXACTO de tu UI Builder
        btnOjos = root.Q<Button>("BtnOjos");
        btnColor = root.Q<Button>("BtnColor");
        btnRopa = root.Q<Button>("BtnRopa");
        contenedorOpciones = root.Q<VisualElement>("ContenedorOpciones");

        // 3. Les ponemos su "oreja" para escuchar los clics de cada sección
        btnOjos.RegisterCallback<ClickEvent>(MostrarOpcionesOjos);
        btnColor.RegisterCallback<ClickEvent>(MostrarOpcionesColor);
        btnRopa.RegisterCallback<ClickEvent>(MostrarOpcionesRopa);

        // 4. Ponemos los botones en su estado "apagado" al iniciar
        ResetearBotones();

        // 5. Si le pusiste datos de la isla en el Inspector, los cargamos
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

    // --- FUNCIONES DE LOS CLICS ---

    private void MostrarOpcionesOjos(ClickEvent evt)
    {
        ResetearBotones(); 
        if (imgOjosActivo != null) btnOjos.style.backgroundImage = new StyleBackground(imgOjosActivo);
        contenedorOpciones.Clear(); 
        
        if (islaActual.opcionesOjos != null)
        {
            foreach (Sprite miSprite in islaActual.opcionesOjos)
            {
                CrearPosterDinamico(miSprite, "150"); // Precio para la Isla 1
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

    // --- EL CREADOR DE ESTANDARTES MAGICO ---

    private void CrearPosterDinamico(Sprite imagenEstandarte, string precioTexto)
    {
        // 1. Clonamos la plantilla que armaste en el UI Builder
        TemplateContainer instancia = plantillaItemTienda.Instantiate();

        // 2. Buscamos las partes adentro del clon
        VisualElement fondo = instancia.Q<VisualElement>("FondoEstandarte");
        Label etiquetaPrecio = instancia.Q<Label>("Precio");
        Button btnCompra = instancia.Q<Button>("BtnCompra");
        VisualElement bannerVendido = instancia.Q<VisualElement>("SoldOut"); // El letrero

        // 3. Le asignamos la imagen correspondiente
        if (fondo != null && imagenEstandarte != null)
        {
            fondo.style.backgroundImage = new StyleBackground(imagenEstandarte);
        }
        
        // 4. Le ponemos el precio visual
        if (etiquetaPrecio != null)
        {
            etiquetaPrecio.text = precioTexto;
        }

        // --- CONEXIÓN DE COMPRA DINÁMICA ---
        // Aquí le decimos al botón recién nacido qué hacer cuando le den clic
        if (btnCompra != null && sistemaCompra != null)
        {
            int precioNumerico = int.Parse(precioTexto); // Convertimos el texto a número para la resta
            btnCompra.clicked += () => sistemaCompra.IntentarCompra(precioNumerico, btnCompra, bannerVendido);
        }

        // Diseño
        instancia.style.marginRight = 15;
        instancia.style.marginLeft = 15;

        // 5. Metemos el clon ya terminado a la pantalla del juego
        contenedorOpciones.Add(instancia);
    }

    void OnDisable()
    {
        btnOjos.UnregisterCallback<ClickEvent>(MostrarOpcionesOjos);
        btnColor.UnregisterCallback<ClickEvent>(MostrarOpcionesColor);
        btnRopa.UnregisterCallback<ClickEvent>(MostrarOpcionesRopa);
    }
}