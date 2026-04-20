using UnityEngine;

public class PlayerVisual : MonoBehaviour
{
    [Header("Renderers")]
    [SerializeField] private SpriteRenderer faceRenderer;
    [SerializeField] private SpriteRenderer eyesRenderer;
    [SerializeField] private SpriteRenderer outfitRenderer;

    [Header("Assets")]
    [SerializeField] private Sprite[] faceColors;
    [SerializeField] private Sprite[] eyes;
    [SerializeField] private Sprite[] eyesWhite;
    [SerializeField] private Sprite[] outfits;
    [SerializeField] private int defaultFace = 0;
    [SerializeField] private int defaultEyes = 0;
    [SerializeField] private int defaultOutfit = 0;
    [SerializeField] private int blackFaceIndex = 9;
    void Start()
    {
        ApplyCustomization();
    }

    public void ApplyCustomization()
    {
        if (PlayerCustomizationManager.Instance == null)
        {
            Debug.LogWarning("No hay CustomizationManager, usando defaults locales");
            ApplyLocalDefaults();
            return;
        }

        var data = PlayerCustomizationManager.Instance;

        int faceIndex = ClampIndex(data.selectedFaceColor, faceColors.Length);
        int eyesIndex = ClampIndex(data.selectedEyes, eyes.Length);
        int outfitIndex = ClampIndex(data.selectedOutfit, outfits.Length);

        // Cara
        faceRenderer.sprite = faceColors[faceIndex];

        // Ojos (regla especial)
        if (faceIndex == blackFaceIndex)
        {
            eyesRenderer.sprite = eyesWhite[ClampIndex(eyesIndex, eyesWhite.Length)];
        }
        else
        {
            eyesRenderer.sprite = eyes[ClampIndex(eyesIndex, eyes.Length)];
        }

            // Traje
            outfitRenderer.sprite = outfits[outfitIndex];
    }

    void ApplyLocalDefaults()
    {
        if (faceColors.Length > 0)
            faceRenderer.sprite = faceColors[0];

        if (eyes.Length > 0)
            eyesRenderer.sprite = eyes[0];

        if (outfits.Length > 0)
            outfitRenderer.sprite = outfits[0];
    }

    int ClampIndex(int index, int length)
    {
        if (length == 0) return 0;
        return Mathf.Clamp(index, 0, length - 1);
    }
}