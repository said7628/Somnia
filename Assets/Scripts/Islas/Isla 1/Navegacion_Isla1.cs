using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class Navegacion_Isla1 : MonoBehaviour
{
    [Header("DB Level IDs")]
    [SerializeField] private int nivel1DbId = 1;
    [SerializeField] private int nivel2DbId = 2;
    [SerializeField] private int nivel3DbId = 3;

    private UIDocument documentoUI;

    private Button botonTienda;
    private Button botonInventario;
    private Button botonMenu;
    private Button botonNivel1;
    private Button botonNivel2;
    private Button botonNivel3;

    private Label textoMonedas;

    private void OnEnable()
    {
        documentoUI = GetComponent<UIDocument>();
        if (documentoUI == null)
        {
            Debug.LogError("[Navegacion_Isla1] UIDocument not found.");
            return;
        }

        var root = documentoUI.rootVisualElement;

        botonTienda = root.Q<Button>("BotonTienda");
        botonInventario = root.Q<Button>("BotonInventario");
        botonMenu = root.Q<Button>("BotonMenu");

        botonNivel1 = root.Q<Button>("Nivel1");
        botonNivel2 = root.Q<Button>("Nivel2");
        botonNivel3 = root.Q<Button>("Nivel3");

        textoMonedas = root.Q<Label>("CantidadMonedas");

        if (botonTienda != null) botonTienda.clicked += IrATienda;
        if (botonInventario != null) botonInventario.clicked += IrAInventario;
        if (botonMenu != null) botonMenu.clicked += IrAMenu;

        if (botonNivel1 != null) botonNivel1.clicked += IrANivel1;
        if (botonNivel2 != null) botonNivel2.clicked += IrANivel2;
        if (botonNivel3 != null) botonNivel3.clicked += IrANivel3;

        ActualizarInterfaz();
        RefreshLevelButtons();
    }

    private void Update()
    {
        RefreshLevelButtons();
    }

    private void ActualizarInterfaz()
    {
        if (textoMonedas != null)
        {
            textoMonedas.text = Memoria_Islas.misMonedas.ToString();
        }
    }

    private void RefreshLevelButtons()
    {
        if (IslandLevelManager.Instance == null || !IslandLevelManager.Instance.IsInitialized)
        {
            SetButtonState(botonNivel1, false, "Level 1 loading...");
            SetButtonState(botonNivel2, false, "Level 2 loading...");
            SetButtonState(botonNivel3, false, "Level 3 loading...");
            return;
        }

        bool canLevel1 = IslandLevelManager.Instance.CanEnterLevel(nivel1DbId);
        bool canLevel2 = IslandLevelManager.Instance.CanEnterLevel(nivel2DbId);
        bool canLevel3 = IslandLevelManager.Instance.CanEnterLevel(nivel3DbId);

        SetButtonState(botonNivel1, canLevel1, canLevel1 ? string.Empty : "Level 1 locked");
        SetButtonState(botonNivel2, canLevel2, canLevel2 ? string.Empty : "Level 2 locked");
        SetButtonState(botonNivel3, canLevel3, canLevel3 ? string.Empty : "Level 3 locked");
    }

    private static void SetButtonState(Button button, bool enabled, string tooltip)
    {
        if (button == null)
        {
            return;
        }

        button.SetEnabled(enabled);
        button.tooltip = tooltip;
    }

    private void IrATienda()
    {
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

    private void IrANivel1()
    {
        TryOpenLevel(nivel1DbId);
    }

    private void IrANivel2()
    {
        TryOpenLevel(nivel2DbId);
    }

    private void IrANivel3()
    {
        TryOpenLevel(nivel3DbId);
    }

    private static void TryOpenLevel(int levelId)
    {
        if (IslandLevelManager.Instance == null)
        {
            Debug.LogWarning("[Navegacion_Isla1] IslandLevelManager not found.");
            return;
        }

        IslandLevelManager.Instance.TryEnterLevel(levelId);
    }

    private void OnDisable()
    {
        if (botonTienda != null) botonTienda.clicked -= IrATienda;
        if (botonInventario != null) botonInventario.clicked -= IrAInventario;
        if (botonMenu != null) botonMenu.clicked -= IrAMenu;
        if (botonNivel1 != null) botonNivel1.clicked -= IrANivel1;
        if (botonNivel2 != null) botonNivel2.clicked -= IrANivel2;
        if (botonNivel3 != null) botonNivel3.clicked -= IrANivel3;
    }
}