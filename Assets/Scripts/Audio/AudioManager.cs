using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instancia;

    [Header("Mixer")]
    [SerializeField] private AudioMixer audioMixer;

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

        CargarVolumenes();
    }

    // MASTER (audio general)
    public void SetMasterLevel(int nivel)
    {
        nivel = Mathf.Clamp(nivel, 0, 5);
        float db = NivelADb(nivel);

        audioMixer.SetFloat("MasterVolume", db);
        PlayerPrefs.SetInt("NivelMaster", nivel);
    }

    // MÚSICA
    public void SetMusicLevel(int nivel)
    {
        nivel = Mathf.Clamp(nivel, 0, 5);
        float db = NivelADb(nivel);

        audioMixer.SetFloat("MusicVolume", db);
        PlayerPrefs.SetInt("NivelMusica", nivel);
    }

    // GETTERS (para que UI se sincronice)
    public int GetMasterLevel()
    {
        return PlayerPrefs.GetInt("NivelMaster", 5);
    }

    public int GetMusicLevel()
    {
        return PlayerPrefs.GetInt("NivelMusica", 5);
    }

    // Cargar al iniciar
    private void CargarVolumenes()
    {
        SetMasterLevel(GetMasterLevel());
        SetMusicLevel(GetMusicLevel());
    }

    // 🎚️ Conversión a decibeles
    private float NivelADb(int nivel)
    {
        switch (nivel)
        {
            case 0: return -80f; // mute
            case 1: return -25f;
            case 2: return -18f;
            case 3: return -12f;
            case 4: return -6f;
            case 5: return 0f;  // máximo
            default: return 0f;
        }
    }
}