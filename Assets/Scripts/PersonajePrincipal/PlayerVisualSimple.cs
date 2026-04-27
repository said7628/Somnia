using System.Collections.Generic;
using UnityEngine;

public class PlayerVisualSimple : MonoBehaviour
{
    [System.Serializable]
    public class PlayerColorSpriteEntry
    {
        public int idItem;
        public Sprite faceSprite;
    }

    [System.Serializable]
    public class PlayerEyesSpriteEntry
    {
        public int idItem;
        public Sprite eyesSprite;
        public Sprite eyesWhiteSprite;
    }

    [System.Serializable]
    public class PlayerOutfitSpriteEntry
    {
        public int idItem;
        public Sprite outfitSprite;
    }

    [Header("Renderers")]
    [SerializeField] private SpriteRenderer faceRenderer;
    [SerializeField] private SpriteRenderer eyesRenderer;
    [SerializeField] private SpriteRenderer outfitRenderer;

    [Header("Color By Item Id")]
    [SerializeField] private PlayerColorSpriteEntry[] colorByItemId;

    [Header("Eyes By Item Id")]
    [SerializeField] private PlayerEyesSpriteEntry[] eyesByItemId;

    [Header("Outfit By Item Id")]
    [SerializeField] private PlayerOutfitSpriteEntry[] outfitByItemId;

    private readonly Dictionary<int, PlayerColorSpriteEntry> colorMap = new();
    private readonly Dictionary<int, PlayerEyesSpriteEntry> eyesMap = new();
    private readonly Dictionary<int, PlayerOutfitSpriteEntry> outfitMap = new();

    private PlayerCustomizationManager data;
    private int currentColorItemId = 1;

    private void Awake()
    {
        BuildMaps();
    }

    private void Start()
    {
        data = FindObjectOfType<PlayerCustomizationManager>();
        ApplyCustomization();
    }

    public void ApplyCustomization()
    {
        if (data == null)
        {
            data = FindObjectOfType<PlayerCustomizationManager>();
        }

        if (data == null)
        {
            Debug.LogError("[PlayerVisualSimple] ERROR missing PlayerCustomizationManager");
            return;
        }

        ApplyColorByItemId(data.selectedFaceColorItemId);
        ApplyEyesByItemId(data.selectedEyesItemId);
        ApplyOutfitByItemId(data.selectedOutfitItemId);
    }

    public void ApplyColorByItemId(int colorId)
    {
        bool found = colorMap.TryGetValue(colorId, out PlayerColorSpriteEntry entry) && entry.faceSprite != null;
        string spriteName = found ? entry.faceSprite.name : "null";

        Debug.Log($"[PlayerVisualSimple] ApplyColor id={colorId} found={found} sprite={spriteName}");

        if (!found)
        {
            Debug.LogError($"[PlayerVisualSimple] ERROR missing color mapping id={colorId}");
            return;
        }

        currentColorItemId = colorId;
        faceRenderer.sprite = entry.faceSprite;
    }

    public void ApplyEyesByItemId(int eyesId)
    {
        bool found = eyesMap.TryGetValue(eyesId, out PlayerEyesSpriteEntry entry);
        if (!found)
        {
            Debug.Log($"[PlayerVisualSimple] ApplyEyes id={eyesId} found=False eyes=null");
            Debug.LogError($"[PlayerVisualSimple] ERROR missing eyes mapping id={eyesId}");
            return;
        }

        bool useWhite = currentColorItemId == 10;
        Sprite sprite = useWhite
            ? (entry.eyesWhiteSprite != null ? entry.eyesWhiteSprite : entry.eyesSprite)
            : entry.eyesSprite;
        bool hasSprite = sprite != null;
        string spriteName = hasSprite ? sprite.name : "null";

        Debug.Log($"[PlayerVisualSimple] ApplyEyes id={eyesId} found={hasSprite} eyes={spriteName}");

        if (!hasSprite)
        {
            Debug.LogError($"[PlayerVisualSimple] ERROR missing eyes mapping id={eyesId}");
            return;
        }

        eyesRenderer.sprite = sprite;
    }

    public void ApplyOutfitByItemId(int outfitId)
    {
        bool found = outfitMap.TryGetValue(outfitId, out PlayerOutfitSpriteEntry entry) && entry.outfitSprite != null;
        string spriteName = found ? entry.outfitSprite.name : "null";

        Debug.Log($"[PlayerVisualSimple] ApplyOutfit id={outfitId} found={found} sprite={spriteName}");

        if (!found)
        {
            Debug.LogError($"[PlayerVisualSimple] ERROR missing outfit mapping id={outfitId}");
            return;
        }

        outfitRenderer.sprite = entry.outfitSprite;
    }

    private void BuildMaps()
    {
        colorMap.Clear();
        eyesMap.Clear();
        outfitMap.Clear();

        if (colorByItemId != null)
        {
            foreach (PlayerColorSpriteEntry entry in colorByItemId)
            {
                if (entry == null)
                {
                    continue;
                }

                colorMap[entry.idItem] = entry;
                string spriteName = entry.faceSprite != null ? entry.faceSprite.name : "null";
                Debug.Log($"[PlayerVisualSimple] Color map id={entry.idItem} sprite={spriteName}");
            }
        }

        if (eyesByItemId != null)
        {
            foreach (PlayerEyesSpriteEntry entry in eyesByItemId)
            {
                if (entry == null)
                {
                    continue;
                }

                eyesMap[entry.idItem] = entry;
                string eyesName = entry.eyesSprite != null ? entry.eyesSprite.name : "null";
                Debug.Log($"[PlayerVisualSimple] Eyes map id={entry.idItem} eyes={eyesName}");
            }
        }

        if (outfitByItemId != null)
        {
            foreach (PlayerOutfitSpriteEntry entry in outfitByItemId)
            {
                if (entry == null)
                {
                    continue;
                }

                outfitMap[entry.idItem] = entry;
                string spriteName = entry.outfitSprite != null ? entry.outfitSprite.name : "null";
                Debug.Log($"[PlayerVisualSimple] Outfit map id={entry.idItem} sprite={spriteName}");
            }
        }
    }
}