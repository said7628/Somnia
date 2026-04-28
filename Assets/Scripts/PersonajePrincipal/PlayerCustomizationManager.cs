using System.Collections.Generic;
using System.Threading.Tasks;
using Somnia.Economy.Core;
using Somnia.Economy.Interfaces;
using Somnia.UnityClient;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    private static readonly Dictionary<int, int> ColorIndexByItemId = new()
    {
        [1] = 0,
        [2] = 4,
        [3] = 3,
        [4] = 1,
        [5] = 5,
        [6] = 7,
        [7] = 8,
        [8] = 6,
        [9] = 2,
        [10] = 9
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

    private IGameDataService gameDataService;

    private void Awake()
    {
        Instance = this;
        if (usarValoresDelInspector)
        {
            Debug.Log("Usando valores del Inspector");
            return;
        }

        LoadData();
    }

    private async void Start()
    {
        ApplyToPlayer();

        if (SceneManager.GetActiveScene().name.StartsWith("Isla"))
        {
            await LoadGameplayEquipmentAsync();
        }
    }

    private void LoadData()
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

    private void SetDefaults()
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
        selectedFaceColor = ResolveIndexForLegacy(selectedFaceColorItemId, ColorIndexByItemId, DefaultColorItemId);
        Debug.Log($"[PlayerCustomization] Color applied id={selectedFaceColorItemId} visualIndex={selectedFaceColor}");
    }

    public void ApplyEyes(int idItem)
    {
        selectedEyesItemId = ResolveItemIdOrDefault(idItem, EyesIndexByItemId, DefaultEyesItemId);
        selectedEyes = ResolveIndexForLegacy(selectedEyesItemId, EyesIndexByItemId, DefaultEyesItemId);
        Debug.Log($"[PlayerCustomization] Eyes applied id={selectedEyesItemId} visualIndex={selectedEyes}");
    }

    public void ApplyOutfit(int idItem)
    {
        selectedOutfitItemId = ResolveItemIdOrDefault(idItem, OutfitIndexByItemId, DefaultOutfitItemId);
        selectedOutfit = ResolveIndexForLegacy(selectedOutfitItemId, OutfitIndexByItemId, DefaultOutfitItemId);
        Debug.Log($"[PlayerCustomization] Outfit applied id={selectedOutfitItemId} visualIndex={selectedOutfit}");
    }

    private async Task LoadGameplayEquipmentAsync()
    {
        Debug.Log("[PlayerCustomization] Loading equipped cosmetics for gameplay");

        bool resolved = await ResolveGameDataServiceAsync();
        if (!resolved)
        {
            Debug.LogWarning("[PlayerCustomization] No se pudo resolver GameDataService. Se usan IDs actuales en memoria.");
            ApplyToPlayer();
            return;
        }

        int slot = Mathf.Max(1, GameSessionManager.Instance?.CurrentSlotNumber ?? 1);
        var response = await gameDataService.GetEquippedItemsAsync(slot);
        if (!response.success || response.data == null)
        {
            Debug.LogWarning($"[PlayerCustomization] No se pudo cargar equipamiento desde backend (slot={slot}): {response.message}");
            ApplyToPlayer();
            return;
        }

        int colorId = response.data.id_item_color;
        int eyesId = response.data.id_item_cara;
        int outfitId = response.data.id_item_outfit;
        Debug.Log($"[PlayerCustomization] Equipment from backend color={colorId} eyes={eyesId} outfit={outfitId}");

        ApplyEquipment(colorId, eyesId, outfitId);
    }

    private async Task<bool> ResolveGameDataServiceAsync()
    {
        if (gameDataService != null)
        {
            return true;
        }

        EconomyModule module = EconomyModule.Instance ?? FindFirstObjectByType<EconomyModule>(FindObjectsInactive.Include);
        if (module == null)
        {
            module = new GameObject("[EconomyModule]").AddComponent<EconomyModule>();
        }

        const float timeoutSeconds = 6f;
        float elapsed = 0f;
        while (module != null && module.GameDataService == null && elapsed < timeoutSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            await Task.Yield();
        }

        gameDataService = module?.GameDataService;
        return gameDataService != null;
    }

    private static int ResolveIndexForLegacy(int itemId, Dictionary<int, int> map, int defaultItemId)
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

        Debug.LogWarning($"[PlayerCustomization] itemId desconocido={itemId}, se usa default={defaultItemId}");
        return defaultItemId;
    }

    private void ApplyToPlayer()
    {
        PlayerVisual player = FindObjectOfType<PlayerVisual>();

        if (player != null)
        {
            player.ApplyColorByItemId(selectedFaceColorItemId);
            player.ApplyEyesByItemId(selectedEyesItemId);
            player.ApplyOutfitByItemId(selectedOutfitItemId);
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