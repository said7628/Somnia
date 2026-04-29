using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instancia;

    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Música")]
    [SerializeField] private AudioClip musicaMenu;
    [SerializeField] private AudioClip musicaTextyping;
    [SerializeField] private AudioClip musicaMapa1;
    [SerializeField] private AudioClip musicaMapa2;
    [SerializeField] private AudioClip musicaMapa3;
    [SerializeField] private AudioClip musicaPlataforma1y2;
    [SerializeField] private AudioClip musicaPlataforma3y4;
    [SerializeField] private AudioClip musicaPlataforma5;

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
                return musicaMenu;

            case "TextTyping":
            case "TextTyping2":
            case "TextTyping3":
            case "TextTyping4":
            case "TextTyping5":
            case "TextTyping6":
            case "ScoreTextyping":
            case "ScoreTextypingFallido":
                return musicaTextyping;

            case "Isla1":
                return musicaMapa1;

            case "Isla2":
                return musicaMapa2;

            case "Isla3":
                return musicaMapa3;

            case "Plataforma1":
            case "Plataforma2":
                return musicaPlataforma1y2;

            case "Plataforma3":
            case "Plataforma4":
                return musicaPlataforma3y4;

            case "Plataforma5":
                return musicaPlataforma5;

            default:
                return musicaMenu;
        }
    }
}