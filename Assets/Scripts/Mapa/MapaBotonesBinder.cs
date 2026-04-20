using System;
using UnityEngine;
using UnityEngine.UIElements;

public class MapaBotonesBinder : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private MapaIslasUI mapaIslasUI;

    [Header("Nombres exactos de los botones en UXML")]
    [SerializeField] private string botonIsla1 = "bosque";
    [SerializeField] private string botonIsla2 = "montana";
    [SerializeField] private string botonIsla3 = "ciudad";

    private void Awake()
    {
        if (uiDocument == null)
        {
            uiDocument = FindFirstObjectByType<UIDocument>();
        }

        if (mapaIslasUI == null)
        {
            mapaIslasUI = FindFirstObjectByType<MapaIslasUI>();
        }
    }

    private void Start()
    {
        Vincular();
    }

    private void Vincular()
    {
        if (uiDocument == null)
        {
            Debug.LogError("MapaBotonesBinder: no hay UIDocument");
            return;
        }

        if (mapaIslasUI == null)
        {
            Debug.LogError("MapaBotonesBinder: no hay referencia a MapaIslasUI");
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;

        Button bosque = root.Q<Button>(botonIsla1);
        Button montana = root.Q<Button>(botonIsla2);
        Button ciudad = root.Q<Button>(botonIsla3);

        ConfigurarBoton(bosque, mapaIslasUI.AbrirIsla1, botonIsla1);
        ConfigurarBoton(montana, mapaIslasUI.MostrarIslaBloqueada, botonIsla2);
        ConfigurarBoton(ciudad, mapaIslasUI.MostrarIslaBloqueada, botonIsla3);
    }

    private void ConfigurarBoton(Button boton, Action accion, string nombre)
    {
        if (boton == null)
        {
            Debug.LogWarning("No se encontro el boton: " + nombre);
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

        HacerHijosNoInterceptables(boton);

        boton.clicked -= accion;
        boton.clicked += accion;

        Debug.Log("Boton configurado: " + nombre);
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