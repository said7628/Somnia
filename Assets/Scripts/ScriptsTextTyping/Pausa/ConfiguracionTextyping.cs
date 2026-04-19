using UnityEngine;
using UnityEngine.UI;

public class PantallaVolumenCanvas : MonoBehaviour
{
    [Header("Botones música")]
    [SerializeField] private Button musica0;
    [SerializeField] private Button musica1;
    [SerializeField] private Button musica2;
    [SerializeField] private Button musica3;
    [SerializeField] private Button musica4;
    [SerializeField] private Button musica5;

    [Header("Botones sonido")]
    [SerializeField] private Button volumen0;
    [SerializeField] private Button volumen1;
    [SerializeField] private Button volumen2;
    [SerializeField] private Button volumen3;
    [SerializeField] private Button volumen4;
    [SerializeField] private Button volumen5;

    [Header("Imágenes de barras música")]
    [SerializeField] private Image[] barrasMusica;

    [Header("Imágenes de barras sonido")]
    [SerializeField] private Image[] barrasSonido;

    [Header("Sprites")]
    [SerializeField] private Sprite barraVacia;
    [SerializeField] private Sprite barraLlenaMusica;
    [SerializeField] private Sprite barraLlenaSonido;

    [Header("Niveles")]
    [Range(0, 5)] [SerializeField] private int nivelMusica = 5;
    [Range(0, 5)] [SerializeField] private int nivelSonido = 5;

    private void Start()
    {
        nivelMusica = PlayerPrefs.GetInt("NivelMusica", 5);
        nivelSonido = PlayerPrefs.GetInt("NivelSonido", 5);

        musica0.onClick.AddListener(() => CambiarMusica(0));
        musica1.onClick.AddListener(() => CambiarMusica(1));
        musica2.onClick.AddListener(() => CambiarMusica(2));
        musica3.onClick.AddListener(() => CambiarMusica(3));
        musica4.onClick.AddListener(() => CambiarMusica(4));
        musica5.onClick.AddListener(() => CambiarMusica(5));

        volumen0.onClick.AddListener(() => CambiarSonido(0));
        volumen1.onClick.AddListener(() => CambiarSonido(1));
        volumen2.onClick.AddListener(() => CambiarSonido(2));
        volumen3.onClick.AddListener(() => CambiarSonido(3));
        volumen4.onClick.AddListener(() => CambiarSonido(4));
        volumen5.onClick.AddListener(() => CambiarSonido(5));

        ActualizarVisualMusica();
        ActualizarVisualSonido();

        AplicarVolumenes();
    }

    public void CambiarMusica(int nuevoNivel)
    {
        nivelMusica = nuevoNivel;
        PlayerPrefs.SetInt("NivelMusica", nivelMusica);
        ActualizarVisualMusica();
        AplicarVolumenes();
    }

    public void CambiarSonido(int nuevoNivel)
    {
        nivelSonido = nuevoNivel;
        PlayerPrefs.SetInt("NivelSonido", nivelSonido);
        ActualizarVisualSonido();
        AplicarVolumenes();
    }

    private void ActualizarVisualMusica()
    {
        // Solo afecta barras 1-5
        for (int i = 0; i < barrasMusica.Length; i++)
        {
            barrasMusica[i].sprite = (i < nivelMusica)
                ? barraLlenaMusica
                : barraVacia;
        }
    }

    private void ActualizarVisualSonido()
    {
        // Solo afecta barras 1-5
        for (int i = 0; i < barrasSonido.Length; i++)
        {
            barrasSonido[i].sprite = (i < nivelSonido)
                ? barraLlenaSonido
                : barraVacia;
        }
    }

    private void AplicarVolumenes()
    {
        float volumenMusicaNormalizado = nivelMusica / 5f;
        float volumenSonidoNormalizado = nivelSonido / 5f;

        AudioListener.volume = Mathf.Max(volumenMusicaNormalizado, volumenSonidoNormalizado);

        Debug.Log("Música: " + volumenMusicaNormalizado);
        Debug.Log("Sonido: " + volumenSonidoNormalizado);
    }
}