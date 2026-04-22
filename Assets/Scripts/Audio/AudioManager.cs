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

    // SFX
    public void SetSFXLevel(int nivel)
    {
        nivel = Mathf.Clamp(nivel, 0, 5);
        float db = NivelADb(nivel);

        audioMixer.SetFloat("SFXVolume", db);
        PlayerPrefs.SetInt("NivelSFX", nivel);
    }

    // MÚSICA
    public void SetMusicLevel(int nivel)
    {
        nivel = Mathf.Clamp(nivel, 0, 5);
        float db = NivelADb(nivel);

        audioMixer.SetFloat("MusicVolume", db);
        PlayerPrefs.SetInt("NivelMusica", nivel);
    }

    // GETTERS
    public int GetSFXLevel()
    {
        return PlayerPrefs.GetInt("NivelSFX", 5);
    }

    public int GetMusicLevel()
    {
        return PlayerPrefs.GetInt("NivelMusica", 5);
    }

    // Cargar al iniciar
    private void CargarVolumenes()
    {
        SetSFXLevel(GetSFXLevel());
        SetMusicLevel(GetMusicLevel());
    }

    // Conversión a decibeles
    private float NivelADb(int nivel)
    {
        switch (nivel)
        {
            case 0: return -80f;
            case 1: return -25f;
            case 2: return -18f;
            case 3: return -12f;
            case 4: return -6f;
            case 5: return 0f;
            default: return 0f;
        }
    }
}