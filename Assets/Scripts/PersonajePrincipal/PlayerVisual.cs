using UnityEngine;

public class PlayerVisual : MonoBehaviour
{
    [Header("Animators")]
    [SerializeField] private Animator faceAnimator;
    [SerializeField] private Animator eyesAnimator;
    [SerializeField] private Animator outfitAnimator;

    [Header("Animator Controllers")]
    [SerializeField] private RuntimeAnimatorController[] faceAnimators;
    [SerializeField] private RuntimeAnimatorController[] eyesAnimators;
    [SerializeField] private RuntimeAnimatorController[] eyesWhiteAnimators;
    [SerializeField] private RuntimeAnimatorController[] outfitAnimators;

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

        // FACE
        faceAnimator.runtimeAnimatorController = faceAnimators[data.selectedFaceColor];

        // OUTFIT
        outfitAnimator.runtimeAnimatorController = outfitAnimators[data.selectedOutfit];

        // EYES
        if (data.selectedFaceColor == 9)
        {
            eyesAnimator.runtimeAnimatorController = eyesWhiteAnimators[data.selectedEyes];
        }
        else
        {
            eyesAnimator.runtimeAnimatorController = eyesAnimators[data.selectedEyes];
        }
    }
}