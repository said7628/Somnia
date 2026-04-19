using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class InventarioConfiguracionUI : MonoBehaviour
{
    private VisualElement root;

    private List<VisualElement> barrasVoz = new List<VisualElement>();
    private List<VisualElement> barrasMusica = new List<VisualElement>();

    void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;

        ObtenerReferencias();
        RegistrarEventos();

        LimpiarVoz();
        LimpiarMusica();

        
        int nivelVoz = PlayerPrefs.GetInt("VolumenVoz", 5);
        int nivelMusica = PlayerPrefs.GetInt("VolumenMusica", 5);

        ActualizarVoz(nivelVoz);
        ActualizarMusica(nivelMusica);
    }

    void ObtenerReferencias()
    {
        barrasVoz.Clear();
        barrasMusica.Clear();

        barrasVoz.Add(root.Q<VisualElement>("Voz20"));
        barrasVoz.Add(root.Q<VisualElement>("Voz40"));
        barrasVoz.Add(root.Q<VisualElement>("Voz60"));
        barrasVoz.Add(root.Q<VisualElement>("Voz80"));
        barrasVoz.Add(root.Q<VisualElement>("Voz100"));

        barrasMusica.Add(root.Q<VisualElement>("Musica20"));
        barrasMusica.Add(root.Q<VisualElement>("Musica40"));
        barrasMusica.Add(root.Q<VisualElement>("Musica60"));
        barrasMusica.Add(root.Q<VisualElement>("Musica80"));
        barrasMusica.Add(root.Q<VisualElement>("Musica100"));
    }

    void RegistrarEventos()
    {
        for (int i = 0; i < barrasVoz.Count; i++)
        {
            int nivel = i + 1;

            if (barrasVoz[i] != null)
            {
                barrasVoz[i].RegisterCallback<ClickEvent>(evt => ActualizarVoz(nivel));
            }
        }

        for (int i = 0; i < barrasMusica.Count; i++)
        {
            int nivel = i + 1;

            if (barrasMusica[i] != null)
            {
                barrasMusica[i].RegisterCallback<ClickEvent>(evt => ActualizarMusica(nivel));
            }
        }
    }

    void LimpiarVoz()
    {
        foreach (VisualElement barra in barrasVoz)
        {
            if (barra != null)
            {
                barra.RemoveFromClassList("activaVoz");
            }
        }
    }

    void LimpiarMusica()
    {
        foreach (VisualElement barra in barrasMusica)
        {
            if (barra != null)
            {
                barra.RemoveFromClassList("activaMusica");
            }
        }
    }

    void ActualizarVoz(int nivel)
    {
        LimpiarVoz();

        for (int i = 0; i < barrasVoz.Count; i++)
        {
            if (barrasVoz[i] == null) continue;

            if (i < nivel)
            {
                barrasVoz[i].AddToClassList("activaVoz");
            }
        }

        PlayerPrefs.SetInt("VolumenVoz", nivel);
        PlayerPrefs.Save();

        Debug.Log("Voz: " + (nivel * 20));
    }

    void ActualizarMusica(int nivel)
    {
        LimpiarMusica();

        for (int i = 0; i < barrasMusica.Count; i++)
        {
            if (barrasMusica[i] == null) continue;

            if (i < nivel)
            {
                barrasMusica[i].AddToClassList("activaMusica");
            }
        }

        PlayerPrefs.SetInt("VolumenMusica", nivel);
        PlayerPrefs.Save();

        Debug.Log("Música: " + (nivel * 20));
    }
}