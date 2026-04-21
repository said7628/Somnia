using UnityEngine;

public class PlayerVisual : MonoBehaviour
{
    [Header("Renderers")]
    [SerializeField] private SpriteRenderer faceRenderer;
    [SerializeField] private SpriteRenderer eyesRenderer;
    [SerializeField] private SpriteRenderer outfitRenderer;

    [Header("Sprites")]
    [SerializeField] private Sprite[] faceSprites;
    [SerializeField] private Sprite[] eyesSprites;
    [SerializeField] private Sprite[] eyesWhiteSprites;
    [SerializeField] private Sprite[] outfitSprites;

    private PlayerCustomizationManager data;

    void Start()
    {
        data = FindObjectOfType<PlayerCustomizationManager>();
        // ApplyCustomization();  <-- Le pusimos las diagonales para apagar esta bomba
    }

    public void ApplyCustomization()
    {
        if (data == null)
        {
            Debug.LogError("No se encontró PlayerCustomizationManager");
            return;
        }

        faceRenderer.sprite = faceSprites[data.selectedFaceColor];
        outfitRenderer.sprite = outfitSprites[data.selectedOutfit];

        if (data.selectedFaceColor == 9)
            eyesRenderer.sprite = eyesWhiteSprites[data.selectedEyes];
        else
            eyesRenderer.sprite = eyesSprites[data.selectedEyes];
    }
}