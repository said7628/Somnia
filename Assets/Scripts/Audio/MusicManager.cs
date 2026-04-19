using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instancia;

    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Música")]
    [SerializeField] private AudioClip musicaMenu;
    [SerializeField] private AudioClip musicaGameplay;

    private AudioClip musicaActual;

    private void Awake()
    {
        if (Instancia == null)
        {
            Instancia = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
    }

    private void Start()
    {
        CambiarMusicaSegunEscena(SceneManager.GetActiveScene().name);
    }

    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        CambiarMusicaSegunEscena(escena.name);
    }

    private void CambiarMusicaSegunEscena(string nombreEscena)
    {
        AudioClip nuevaMusica = ObtenerMusicaParaEscena(nombreEscena);

        if (nuevaMusica == null)
            return;

        if (musicaActual == nuevaMusica)
            return;

        musicaActual = nuevaMusica;
        audioSource.clip = musicaActual;
        audioSource.Play();
    }

    private AudioClip ObtenerMusicaParaEscena(string nombreEscena)
    {
        switch (nombreEscena)
        {
            case "Pantalla_principal":
            case "Nuevo juego":
            case "Configuracion":
            case "Mapa":
                return musicaMenu;

            case "Nivel1":
            case "Nivel2":
            case "Nivel3":
                return musicaGameplay;

            default:
                return musicaMenu;
        }
    }
}