using System;
using UnityEngine;
using UnityEngine.UIElements;

public class MapaBotonesBinder : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private MapaIslasUI mapaIslasUI;

    [Header("Nombres exactos de los botones en UXML")]
    [SerializeField] private string botonIsla1 = "bosque";
    [SerializeField] private string botonIsla2 = "nieve";
    [SerializeField] private string botonIsla3 = "ciudad";

    private Button bosqueButton;
    private Button nieveButton;
    private Button ciudadButton;

    private void Awake()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        if (uiDocument == null)
        {
            uiDocument = FindFirstObjectByType<UIDocument>();
        }

        if (mapaIslasUI == null)
        {
            mapaIslasUI = GetComponent<MapaIslasUI>();
        }

        if (mapaIslasUI == null)
        {
            mapaIslasUI = FindFirstObjectByType<MapaIslasUI>();
        }
    }

    private void OnEnable()
    {
        Vincular();
    }

    private void OnDisable()
    {
        Desvincular();
    }

    private void Vincular()
    {
        if (uiDocument == null)
        {
            Debug.LogError("[MapaBotonesBinder] No hay UIDocument.");
            return;
        }

        if (mapaIslasUI == null)
        {
            Debug.LogError("[MapaBotonesBinder] No hay referencia a MapaIslasUI.");
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;

        bosqueButton = root.Q<Button>(botonIsla1);
        nieveButton = root.Q<Button>(botonIsla2);
        ciudadButton = root.Q<Button>(botonIsla3);

        ConfigurarBoton(bosqueButton, mapaIslasUI.OnBosqueClick, botonIsla1);
        ConfigurarBoton(nieveButton, mapaIslasUI.OnNieveClick, botonIsla2);
        ConfigurarBoton(ciudadButton, mapaIslasUI.OnCiudadClick, botonIsla3);
    }

    private void Desvincular()
    {
        QuitarBoton(bosqueButton, mapaIslasUI != null ? mapaIslasUI.OnBosqueClick : null);
        QuitarBoton(nieveButton, mapaIslasUI != null ? mapaIslasUI.OnNieveClick : null);
        QuitarBoton(ciudadButton, mapaIslasUI != null ? mapaIslasUI.OnCiudadClick : null);
    }

    private void ConfigurarBoton(Button boton, Action accion, string nombre)
    {
        if (boton == null)
        {
            Debug.LogWarning("[MapaBotonesBinder] No se encontro el boton: " + nombre);
            return;
        }

        boton.pickingMode = PickingMode.Position;
        boton.style.backgroundColor = new StyleColor(Color.clear);
        boton.style.borderTopWidth = 0;
        boton.style.borderBottomWidth = 0;
        boton.style.borderLeftWidth = 0;
        boton.style.borderRightWidth = 0;
        boton.style.paddingLeft = 0;
        boton.style.paddingRight = 0;
        boton.style.paddingTop = 0;
        boton.style.paddingBottom = 0;
        boton.BringToFront();

        HacerHijosNoInterceptables(boton);

        boton.clicked -= accion;
        boton.clicked += accion;

        Debug.Log("[MapaBotonesBinder] Boton configurado: " + nombre);
    }

    private void QuitarBoton(Button boton, Action accion)
    {
        if (boton == null || accion == null)
        {
            return;
        }

        boton.clicked -= accion;
    }

    private void HacerHijosNoInterceptables(VisualElement padre)
    {
        foreach (VisualElement hijo in padre.Children())
        {
            hijo.pickingMode = PickingMode.Ignore;
            HacerHijosNoInterceptables(hijo);
        }
    }
}