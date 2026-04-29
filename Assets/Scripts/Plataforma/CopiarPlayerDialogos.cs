using UnityEngine;
using UnityEngine.UI;

public class CopiarPlayerVariantAUI : MonoBehaviour
{
    [Header("Player Variant del escenario")]
    [SerializeField] private SpriteRenderer faceRenderer;
    [SerializeField] private SpriteRenderer eyesRenderer;
    [SerializeField] private SpriteRenderer outfitRenderer;

    [Header("Images del Canvas")]
    [SerializeField] private Image faceImage;
    [SerializeField] private Image eyesImage;
    [SerializeField] private Image outfitImage;

    private void OnEnable()
    {
        faceImage.sprite = faceRenderer.sprite;
        eyesImage.sprite = eyesRenderer.sprite;
        outfitImage.sprite = outfitRenderer.sprite;

        faceImage.color = faceRenderer.color;
        eyesImage.color = eyesRenderer.color;
        outfitImage.color = outfitRenderer.color;
    }
}