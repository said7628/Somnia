using UnityEngine;

public class PlayerVisual : MonoBehaviour
{
    [Header("Animators")]
    [SerializeField] private Animator faceAnimator;
    [SerializeField] private Animator outfitAnimator;

    [Header("Eyes (Separados)")]
    [SerializeField] private Animator eyesNormalAnimator;
    [SerializeField] private Animator eyesWhiteAnimator;

    [Header("Animation Clips")]
    [SerializeField] private AnimationClip[] faceClips;
    [SerializeField] private AnimationClip[] outfitClips;
    [SerializeField] private AnimationClip[] eyesClips;
    [SerializeField] private AnimationClip[] eyesWhiteClips;

    private PlayerCustomizationManager data;

    void Start()
    {
        data = FindObjectOfType<PlayerCustomizationManager>();
        ApplyCustomization();
    }

    public void ApplyCustomization()
    {
        if (data == null)
        {
            Debug.LogError("No se encontró PlayerCustomizationManager");
            return;
        }

        // 🔒 PROTECCIÓN DE ÍNDICES
        if (data.selectedFaceColor >= faceClips.Length ||
            data.selectedOutfit >= outfitClips.Length ||
            data.selectedEyes >= eyesClips.Length)
        {
            Debug.LogError("Índice fuera de rango en PlayerVisual");
            return;
        }

        // FACE
        faceAnimator.Play(faceClips[data.selectedFaceColor].name, 0, 0f);

        // OUTFIT
        outfitAnimator.Play(outfitClips[data.selectedOutfit].name, 0, 0f);

        // 🔥 EYES (SOLUCIÓN DEFINITIVA)

        // Apagar ambos primero
        if (eyesNormalAnimator != null)
            eyesNormalAnimator.gameObject.SetActive(false);

        if (eyesWhiteAnimator != null)
            eyesWhiteAnimator.gameObject.SetActive(false);

        // Activar el correcto
        if (data.selectedFaceColor == 9) // 👈 color negro (índice 9)
        {
            if (data.selectedEyes < eyesWhiteClips.Length)
            {
                eyesWhiteAnimator.gameObject.SetActive(true);
                eyesWhiteAnimator.Play(eyesWhiteClips[data.selectedEyes].name, 0, 0f);
            }
            else
            {
                Debug.LogError("Índice fuera de rango en eyesWhiteClips");
            }
        }
        else
        {
            eyesNormalAnimator.gameObject.SetActive(true);
            eyesNormalAnimator.Play(eyesClips[data.selectedEyes].name, 0, 0f);
        }

        Debug.Log("Customización aplicada correctamente");
    }
}