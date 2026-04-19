using UnityEngine;
using UnityEngine.UI;

public class PantallaVolumenCanvas : MonoBehaviour
{
    [Header("Niveles")]
    [SerializeField] private int nivelAudioGeneral = 5;
    [SerializeField] private int nivelMusica = 5;

    [Header("Botones audio general")]
    [SerializeField] private Button[] botonesAudio = new Button[6];

    [Header("Botones música")]
    [SerializeField] private Button[] botonesMusica = new Button[6];

    [Header("Barras audio general")]
    [SerializeField] private Image[] barrasAudio = new Image[5];

    [Header("Barras música")]
    [SerializeField] private Image[] barrasMusica = new Image[5];

    [Header("Sprites")]
    [SerializeField] private Sprite barraVacia;
    [SerializeField] private Sprite barraLlenaAudio;
    [SerializeField] private Sprite barraLlenaMusica;

    private void Start()
    {
        // Cargar niveles desde AudioManager
        if (AudioManager.Instancia != null)
        {
            nivelAudioGeneral = AudioManager.Instancia.GetMasterLevel();
            nivelMusica = AudioManager.Instancia.GetMusicLevel();
        }

        for (int i = 0; i < 6; i++)
        {
            int nivelA = i;
            botonesAudio[i].onClick.AddListener(() => SeleccionarNivelAudio(nivelA));

            int nivelM = i;
            botonesMusica[i].onClick.AddListener(() => SeleccionarNivelMusica(nivelM));
        }

        ActualizarVisualAudio();
        ActualizarVisualMusica();
    }

    public void SeleccionarNivelAudio(int nuevoNivel)
    {
        nivelAudioGeneral = Mathf.Clamp(nuevoNivel, 0, 5);
        ActualizarVisualAudio();

        // Mandar al AudioManager
        if (AudioManager.Instancia != null)
            AudioManager.Instancia.SetMasterLevel(nivelAudioGeneral);
    }

    public void SeleccionarNivelMusica(int nuevoNivel)
    {
        nivelMusica = Mathf.Clamp(nuevoNivel, 0, 5);
        ActualizarVisualMusica();

        // Mandar al AudioManager
        if (AudioManager.Instancia != null)
            AudioManager.Instancia.SetMusicLevel(nivelMusica);
    }

    private void ActualizarVisualAudio()
    {
        for (int i = 0; i < 5; i++)
        {
            barrasAudio[i].sprite = i < nivelAudioGeneral
                ? barraLlenaAudio
                : barraVacia;
        }
    }

    private void ActualizarVisualMusica()
    {
        for (int i = 0; i < 5; i++)
        {
            barrasMusica[i].sprite = i < nivelMusica
                ? barraLlenaMusica
                : barraVacia;
        }
    }
}