using UnityEngine;

public class PlayerVisualSimple : MonoBehaviour
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
        ApplyCustomization();
    }

    public void ApplyCustomization()
    {
        if (data == null)
        {
            Debug.LogError("No se encontró PlayerCustomizationManager");
            return;
        }

        // Protección de índices
        if (data.selectedFaceColor >= faceSprites.Length ||
            data.selectedOutfit >= outfitSprites.Length ||
            data.selectedEyes >= eyesSprites.Length)
        {
            Debug.LogError("Índice fuera de rango en PlayerVisualSimple");
            return;
        }

        // FACE
        faceRenderer.sprite = faceSprites[data.selectedFaceColor];

        // OUTFIT
        outfitRenderer.sprite = outfitSprites[data.selectedOutfit];

        // EYES
        if (data.selectedFaceColor == 9) // color negro
        {
            if (data.selectedEyes < eyesWhiteSprites.Length)
                eyesRenderer.sprite = eyesWhiteSprites[data.selectedEyes];
        }
        else
        {
            eyesRenderer.sprite = eyesSprites[data.selectedEyes];
        }

        Debug.Log("Customización aplicada (modo simple)");
    }
}