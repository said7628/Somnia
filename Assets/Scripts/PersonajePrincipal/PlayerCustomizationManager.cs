using UnityEngine;
using System.Collections.Generic;

public class PlayerCustomizationManager : MonoBehaviour
{
    public static PlayerCustomizationManager Instance;

    [Header("Selección actual")]
    public int selectedFaceColor = 0;
    public int selectedEyes = 0;
    public int selectedOutfit = 0;
    public int selectedFaceColorItemId = 1;
    public int selectedEyesItemId = 11;
    public int selectedOutfitItemId = 18;
    [SerializeField] private bool usarValoresDelInspector = true;

    private const int DefaultColorItemId = 1;
    private const int DefaultEyesItemId = 11;
    private const int DefaultOutfitItemId = 18;

    // IDs de backend -> índice visual real (orden de clips/sprites en PlayerVisual/PlayerVisualSimple):
    // 0 blanco, 1 amarillo, 2 azul, 3 morado, 4 naranja, 5 rojo, 6 rosa, 7 turquesa, 8 verde, 9 negro
    private static readonly Dictionary<int, int> ColorIndexByItemId = new()
    {
        [1] = 0,  // blanco
        [2] = 4,  // naranja
        [3] = 3,  // morado
        [4] = 1,  // amarillo
        [5] = 5,  // rojo
        [6] = 7,  // turquesa
        [7] = 8,  // verde
        [8] = 6,  // rosa
        [9] = 2,  // azul
        [10] = 9  // negro
    };

    private static readonly Dictionary<int, int> EyesIndexByItemId = new()
    {
        [11] = 0,
        [12] = 1,
        [13] = 2,
        [14] = 3,
        [15] = 4,
        [16] = 5,
        [17] = 6
    };

    private static readonly Dictionary<int, int> OutfitIndexByItemId = new()
    {
        [18] = 0,
        [19] = 1,
        [20] = 2,
        [21] = 3
    };

    private static readonly Dictionary<int, string> ItemNameById = new()
    {
        [1] = "blanco",
        [2] = "naranja",
        [3] = "morado",
        [4] = "amarillo",
        [5] = "rojo",
        [6] = "turquesa",
        [7] = "verde",
        [8] = "rosa",
        [9] = "azul",
        [10] = "negro",
        [11] = "ovalos/default",
        [12] = "rombos",
        [13] = "cansado",
        [14] = "estrella",
        [15] = "happy",
        [16] = "pirata",
        [17] = "emputado",
        [18] = "boy scout/default",
        [19] = "engrane",
        [20] = "rana",
        [21] = "diablito"
    };


    void Awake()
    {
        Instance = this;
        if (usarValoresDelInspector)
        {
            Debug.Log("Usando valores del Inspector");
            return;
        }

        LoadData();
    }

    void LoadData()
    {
        if (PlayerPrefs.HasKey("HasCustomization"))
        {
            selectedFaceColor = PlayerPrefs.GetInt("Face");
            selectedEyes = PlayerPrefs.GetInt("Eyes");
            selectedOutfit = PlayerPrefs.GetInt("Outfit");
        }
        else
        {
            SetDefaults();
        }
    }

    public void SaveData()
    {
        PlayerPrefs.SetInt("Face", selectedFaceColor);
        PlayerPrefs.SetInt("Eyes", selectedEyes);
        PlayerPrefs.SetInt("Outfit", selectedOutfit);
        PlayerPrefs.SetInt("HasCustomization", 1);
    }

    void SetDefaults()
    {
        Debug.Log("Aplicando apariencia por defecto");

        selectedFaceColor = 0;
        selectedEyes = 0;
        selectedOutfit = 0;
    }

    public void SetFace(int index)
    {
        selectedFaceColor = index;
        ApplyToPlayer();
    }

    public void SetEyes(int index)
    {
        selectedEyes = index;
        ApplyToPlayer();
    }

    public void SetOutfit(int index)
    {
        selectedOutfit = index;
        ApplyToPlayer();
    }

    public void ApplyEquipment(int colorItemId, int eyesItemId, int outfitItemId)
    {
        Debug.Log($"[PlayerCustomization] Applying color={colorItemId} eyes={eyesItemId} outfit={outfitItemId}");
        ApplyColor(colorItemId);
        ApplyEyes(eyesItemId);
        ApplyOutfit(outfitItemId);
        ApplyToPlayer();
    }

    public void ApplyColor(int idItem)
    {
        selectedFaceColorItemId = ResolveItemIdOrDefault(idItem, ColorIndexByItemId, DefaultColorItemId);
        selectedFaceColor = ResolveIndex(idItem, ColorIndexByItemId, DefaultColorItemId);
        Debug.Log($"[PlayerCustomization] Color applied id={selectedFaceColorItemId} name={ResolveItemName(selectedFaceColorItemId)} visualIndex={selectedFaceColor}");
    }

    public void ApplyEyes(int idItem)
    {
        selectedEyesItemId = ResolveItemIdOrDefault(idItem, EyesIndexByItemId, DefaultEyesItemId);
        selectedEyes = ResolveIndex(idItem, EyesIndexByItemId, DefaultEyesItemId);
        Debug.Log($"[PlayerCustomization] Eyes id={selectedEyesItemId} -> {ResolveItemName(selectedEyesItemId)} visualIndex={selectedEyes}");
    }

    public void ApplyOutfit(int idItem)
    {
        selectedOutfitItemId = ResolveItemIdOrDefault(idItem, OutfitIndexByItemId, DefaultOutfitItemId);
        selectedOutfit = ResolveIndex(idItem, OutfitIndexByItemId, DefaultOutfitItemId);
        Debug.Log($"[PlayerCustomization] Outfit id={selectedOutfitItemId} -> {ResolveItemName(selectedOutfitItemId)} visualIndex={selectedOutfit}");
    }

    private static int ResolveIndex(int itemId, Dictionary<int, int> map, int defaultItemId)
    {
        if (map.TryGetValue(itemId, out int index))
        {
            return index;
        }

        Debug.LogWarning($"[PlayerCustomization] itemId desconocido={itemId}, se usa default={defaultItemId}");
        return map[defaultItemId];
    }

    private static int ResolveItemIdOrDefault(int itemId, Dictionary<int, int> map, int defaultItemId)
    {
        if (map.ContainsKey(itemId))
        {
            return itemId;
        }

        return defaultItemId;
    }

    private static string ResolveItemName(int itemId)
    {
        return ItemNameById.TryGetValue(itemId, out string itemName) ? itemName : "unknown";
    }

    void ApplyToPlayer()
    {
        PlayerVisual player = FindObjectOfType<PlayerVisual>();

        if (player != null)
        {
            player.ApplyCustomization();
        }
        else
        {
            Debug.LogWarning("[PlayerCustomization] PlayerVisual no encontrado");
        }

        PlayerVisualSimple simplePlayer = FindObjectOfType<PlayerVisualSimple>();
        if (simplePlayer != null)
        {
            simplePlayer.ApplyColorByItemId(selectedFaceColorItemId);
            simplePlayer.ApplyEyesByItemId(selectedEyesItemId);
            simplePlayer.ApplyOutfitByItemId(selectedOutfitItemId);
        }
    }
}