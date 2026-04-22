using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class InventarioConfiguracionUI : MonoBehaviour
{
    private VisualElement root;

    private VisualElement voz0;
    private VisualElement musica0;

    private List<VisualElement> barrasVoz = new List<VisualElement>();
    private List<VisualElement> barrasMusica = new List<VisualElement>();

    private void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;

        ObtenerReferencias();
        RegistrarEventos();

        int nivelVoz = 5;
        int nivelMusica = 5;

        if (AudioManager.Instancia != null)
        {
            nivelVoz = AudioManager.Instancia.GetSFXLevel();
            nivelMusica = AudioManager.Instancia.GetMusicLevel();
        }

        ActualizarVoz(nivelVoz);
        ActualizarMusica(nivelMusica);
    }

    private void ObtenerReferencias()
    {
        barrasVoz.Clear();
        barrasMusica.Clear();

        voz0 = root.Q<VisualElement>("Voz0");
        musica0 = root.Q<VisualElement>("Musica0");

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

    private void RegistrarEventos()
    {
        if (voz0 != null)
        {
            voz0.RegisterCallback<ClickEvent>(evt => ActualizarVoz(0));
        }

        if (musica0 != null)
        {
            musica0.RegisterCallback<ClickEvent>(evt => ActualizarMusica(0));
        }

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

    private void LimpiarVoz()
    {
        foreach (VisualElement barra in barrasVoz)
        {
            if (barra != null)
            {
                barra.RemoveFromClassList("activaVoz");
            }
        }
    }

    private void LimpiarMusica()
    {
        foreach (VisualElement barra in barrasMusica)
        {
            if (barra != null)
            {
                barra.RemoveFromClassList("activaMusica");
            }
        }
    }

    private void ActualizarVoz(int nivel)
    {
        nivel = Mathf.Clamp(nivel, 0, 5);

        LimpiarVoz();

        for (int i = 0; i < barrasVoz.Count; i++)
        {
            if (barrasVoz[i] == null) continue;

            if (i < nivel)
            {
                barrasVoz[i].AddToClassList("activaVoz");
            }
        }

        if (AudioManager.Instancia != null)
            AudioManager.Instancia.SetSFXLevel(nivel);

        Debug.Log("SFX: " + nivel);
    }

    private void ActualizarMusica(int nivel)
    {
        nivel = Mathf.Clamp(nivel, 0, 5);

        LimpiarMusica();

        for (int i = 0; i < barrasMusica.Count; i++)
        {
            if (barrasMusica[i] == null) continue;

            if (i < nivel)
            {
                barrasMusica[i].AddToClassList("activaMusica");
            }
        }

        if (AudioManager.Instancia != null)
            AudioManager.Instancia.SetMusicLevel(nivel);

        Debug.Log("Música: " + nivel);
    }
}