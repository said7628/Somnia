using UnityEngine;

public class PlayerVisualSimple : MonoBehaviour
{
    [System.Serializable]
    private class PlayerCosmeticSpriteEntry
    {
        public int idItem;
        public Sprite visual;
        public Sprite visualWhite;
    }

    [Header("Renderers")]
    [SerializeField] private SpriteRenderer faceRenderer;
    [SerializeField] private SpriteRenderer eyesRenderer;
    [SerializeField] private SpriteRenderer outfitRenderer;

    [Header("Sprites")]
    [SerializeField] private Sprite[] faceSprites;
    [SerializeField] private Sprite[] eyesSprites;
    [SerializeField] private Sprite[] eyesWhiteSprites;
    [SerializeField] private Sprite[] outfitSprites;
    [Header("Mapeo explícito por id_item (opcional, recomendado)")]
    [SerializeField] private PlayerCosmeticSpriteEntry[] eyesVisualByItem;
    [SerializeField] private PlayerCosmeticSpriteEntry[] outfitVisualByItem;

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
        Sprite resolvedOutfit = ResolveOutfitSprite();
        outfitRenderer.sprite = resolvedOutfit != null ? resolvedOutfit : outfitSprites[data.selectedOutfit];

        // EYES
        if (data.selectedFaceColor == 9) // color negro
        {
            Sprite resolvedEyesWhite = ResolveEyesSprite(true);
            if (resolvedEyesWhite != null)
            {
                eyesRenderer.sprite = resolvedEyesWhite;
            }
            else if (data.selectedEyes < eyesWhiteSprites.Length)
            {
                eyesRenderer.sprite = eyesWhiteSprites[data.selectedEyes];
            }
        }
        else
        {
            Sprite resolvedEyes = ResolveEyesSprite(false);
            eyesRenderer.sprite = resolvedEyes != null ? resolvedEyes : eyesSprites[data.selectedEyes];
        }

        Debug.Log("Customización aplicada (modo simple)");
    }

    private Sprite ResolveOutfitSprite()
    {
        int outfitId = data.selectedOutfitItemId;
        if (TryGetEntry(outfitVisualByItem, outfitId, out PlayerCosmeticSpriteEntry entry) && entry.visual != null)
        {
            Debug.Log($"[PlayerCustomization] Outfit id={outfitId} -> {ResolveItemName(outfitId)} visual={entry.visual.name}");
            return entry.visual;
        }

        return null;
    }

    private Sprite ResolveEyesSprite(bool useWhite)
    {
        int eyesId = data.selectedEyesItemId;
        if (TryGetEntry(eyesVisualByItem, eyesId, out PlayerCosmeticSpriteEntry entry))
        {
            Sprite sprite = useWhite ? (entry.visualWhite != null ? entry.visualWhite : entry.visual) : entry.visual;
            if (sprite != null)
            {
                Debug.Log($"[PlayerCustomization] Eyes id={eyesId} -> {ResolveItemName(eyesId)} visual={sprite.name}");
                return sprite;
            }
        }

        return null;
    }

    private static bool TryGetEntry(PlayerCosmeticSpriteEntry[] entries, int idItem, out PlayerCosmeticSpriteEntry entry)
    {
        if (entries != null)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i] != null && entries[i].idItem == idItem)
                {
                    entry = entries[i];
                    return true;
                }
            }
        }

        entry = null;
        return false;
    }

    private static string ResolveItemName(int itemId)
    {
        return itemId switch
        {
            11 => "ovalos",
            12 => "rombos",
            13 => "cansado",
            14 => "estrella",
            15 => "happy",
            16 => "pirata",
            17 => "emputado",
            18 => "boy scout/base",
            19 => "engrane",
            20 => "rana",
            21 => "diablito",
            _ => "unknown"
        };
    }
}