using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SceneAudioOnLoad : MonoBehaviour
{
    [SerializeField] private AudioClip clipToPlay;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    private void Start()
    {
        if (clipToPlay != null)
        {
            audioSource.PlayOneShot(clipToPlay);
        }
    }
}
