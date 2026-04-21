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
    
    // ¡Ojo aquí! Las variables en C# deben empezar con minúscula (btn en vez de Btn)
    private Button btnOjos;
    private Button btnColor;
    private Button btnRopa;
    private VisualElement contenedorOpciones;

    void OnEnable()
    {
        // 1. Conectamos con el documento UI de tu escena
        tiendaUI = GetComponent<UIDocument>();
        var root = tiendaUI.rootVisualElement;

        // 2. Buscamos los botones usando el nombre EXACTO de tu UI Builder (Con mayúscula adentro)
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
        // Regresa todos los botones a su imagen normal para que no haya dos prendidos a la vez
        if (imgOjosNormal != null) btnOjos.style.backgroundImage = new StyleBackground(imgOjosNormal);
        if (imgColorNormal != null) btnColor.style.backgroundImage = new StyleBackground(imgColorNormal);
        if (imgRopaNormal != null) btnRopa.style.backgroundImage = new StyleBackground(imgRopaNormal);
    }

    // --- FUNCIONES DE LOS CLICS ---

    private void MostrarOpcionesOjos(ClickEvent evt)
    {
        ResetearBotones(); // Apagamos todos primero
        // Prendemos solo el botón de Ojos
        if (imgOjosActivo != null) btnOjos.style.backgroundImage = new StyleBackground(imgOjosActivo);

        contenedorOpciones.Clear(); // Limpiamos la ropa/colores viejos del centro
        
        if (islaActual.opcionesOjos != null)
        {
            // Por cada ojo en tu lista de la base de datos, creamos un estandarte nuevo
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

    // --- EL CREADOR DE ESTANDARTES MAGICO ---

    private void CrearPosterDinamico(Sprite imagenEstandarte, string precioTexto)
    {
        // 1. Clonamos la plantilla que armaste en el UI Builder
        TemplateContainer instancia = plantillaItemTienda.Instantiate();

        // 2. Buscamos las partes adentro del clon
        VisualElement fondo = instancia.Q<VisualElement>("FondoEstandarte");
        Label etiquetaPrecio = instancia.Q<Label>("Precio");
        Button btnCompra = instancia.Q<Button>("BtnCompra");

        // 3. Le asignamos la imagen correspondiente
        if (fondo != null && imagenEstandarte != null)
        {
            fondo.style.backgroundImage = new StyleBackground(imagenEstandarte);
        }
        
        // 4. Le ponemos el precio
        if (etiquetaPrecio != null)
        {
            etiquetaPrecio.text = precioTexto;
        }

        // Le damos un poco de espacio para que no estén pegados nariz con nariz
        instancia.style.marginRight = 15;
        instancia.style.marginLeft = 15;

        // 5. Metemos el clon ya terminado a la pantalla del juego
        contenedorOpciones.Add(instancia);
    }

    // Limpiamos la memoria al salir para evitar bugs (Buenas prácticas)
    void OnDisable()
    {
        btnOjos.UnregisterCallback<ClickEvent>(MostrarOpcionesOjos);
        btnColor.UnregisterCallback<ClickEvent>(MostrarOpcionesColor);
        btnRopa.UnregisterCallback<ClickEvent>(MostrarOpcionesRopa);
    }
}