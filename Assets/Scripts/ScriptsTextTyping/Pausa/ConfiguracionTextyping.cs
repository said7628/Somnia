using UnityEngine;
using UnityEngine.UI;

public class PantallaVolumenCanvas : MonoBehaviour
{
    [Header("Niveles")]
    [SerializeField] private int nivelSFX = 5;
    [SerializeField] private int nivelMusica = 5;

    [Header("Botones SFX")]
    [SerializeField] private Button[] botonesSFX = new Button[6];

    [Header("Botones música")]
    [SerializeField] private Button[] botonesMusica = new Button[6];

    [Header("Barras SFX")]
    [SerializeField] private Image[] barrasSFX = new Image[5];

    [Header("Barras música")]
    [SerializeField] private Image[] barrasMusica = new Image[5];

    [Header("Sprites")]
    [SerializeField] private Sprite barraVacia;
    [SerializeField] private Sprite barraLlenaSFX;
    [SerializeField] private Sprite barraLlenaMusica;

    private void Start()
    {
        if (AudioManager.Instancia != null)
        {
            nivelSFX = AudioManager.Instancia.GetSFXLevel();
            nivelMusica = AudioManager.Instancia.GetMusicLevel();
        }

        for (int i = 0; i < 6; i++)
        {
            int nivelS = i;
            botonesSFX[i].onClick.AddListener(() => SeleccionarNivelSFX(nivelS));

            int nivelM = i;
            botonesMusica[i].onClick.AddListener(() => SeleccionarNivelMusica(nivelM));
        }

        ActualizarVisualSFX();
        ActualizarVisualMusica();
    }

    public void SeleccionarNivelSFX(int nuevoNivel)
    {
        nivelSFX = Mathf.Clamp(nuevoNivel, 0, 5);
        ActualizarVisualSFX();

        if (AudioManager.Instancia != null)
            AudioManager.Instancia.SetSFXLevel(nivelSFX);
    }

    public void SeleccionarNivelMusica(int nuevoNivel)
    {
        nivelMusica = Mathf.Clamp(nuevoNivel, 0, 5);
        ActualizarVisualMusica();

        if (AudioManager.Instancia != null)
            AudioManager.Instancia.SetMusicLevel(nivelMusica);
    }

    private void ActualizarVisualSFX()
    {
        for (int i = 0; i < 5; i++)
        {
            barrasSFX[i].sprite = i < nivelSFX
                ? barraLlenaSFX
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