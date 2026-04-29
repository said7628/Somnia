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
    public System.Action<int, int, int> OnEquipmentApplied;

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
    public bool IsEquipmentReady { get; private set; }

    private void Awake()
    {
        Instance = this;
        bool allowInspectorOnly = usarValoresDelInspector && Application.isEditor && Debug.isDebugBuild;
        if (allowInspectorOnly)
        {
            Debug.Log("Usando valores del Inspector (solo debug editor)");
            return;
        }

        if (usarValoresDelInspector)
        {
            Debug.Log("[PlayerCustomization] Ignorando valores del Inspector en runtime para priorizar equipamiento de sesion/backend.");
        }

        LoadData();
    }

    private async void Start()
    {
        ApplyToPlayer();

        string activeScene = SceneManager.GetActiveScene().name;
        bool isGameplayScene = activeScene.StartsWith("Isla") || activeScene.StartsWith("TextTyping");
        if (isGameplayScene)
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
        IsEquipmentReady = true;
        TextTypingSession.SetEquippedItems(selectedFaceColorItemId, selectedEyesItemId, selectedOutfitItemId);
        ApplyToPlayer();
        OnEquipmentApplied?.Invoke(selectedFaceColorItemId, selectedEyesItemId, selectedOutfitItemId);
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

        int slot = Mathf.Max(1, GameSessionManager.Instance?.CurrentSlotNumber ?? 1);
        bool isTextTyping = SceneManager.GetActiveScene().name.StartsWith("TextTyping");
        if (isTextTyping)
        {
            Debug.Log($"[TextTypingEquipment] Current slot resolved={slot}");
            Debug.Log($"[TextTypingEquipment] Loading equipment for TextTyping slot={slot}");
        }

        if (TryApplySessionEquipmentForTextTyping(isTextTyping))
        {
            // Keep trying backend load afterwards to ensure authoritative slot equipment.
        }

        bool resolved = await ResolveGameDataServiceAsync();
        if (!resolved)
        {
            Debug.LogWarning("[PlayerCustomization] No se pudo resolver GameDataService. Se usan IDs actuales en memoria.");
            if (isTextTyping)
            {
                Debug.LogWarning("[TextTypingEquipment][WARN] No backend equipment found, using session equipment if available");
                if (!TryApplySessionEquipmentForTextTyping(true))
                {
                    Debug.LogWarning("[TextTypingEquipment][WARN] No equipment available, using base fallback color=1 eyes=11 outfit=18");
                    ApplyEquipment(DefaultColorItemId, DefaultEyesItemId, DefaultOutfitItemId);
                }
            }
            else
            {
                ApplyEquipment(DefaultColorItemId, DefaultEyesItemId, DefaultOutfitItemId);
            }
            return;
        }

        var response = await gameDataService.GetEquippedItemsAsync(slot);
        if (!response.success || response.data == null)
        {
            Debug.LogWarning($"[PlayerCustomization] No se pudo cargar equipamiento desde backend (slot={slot}): {response.message}");
            if (isTextTyping)
            {
                Debug.LogWarning("[TextTypingEquipment][WARN] No backend equipment found, using session equipment if available");
                if (!TryApplySessionEquipmentForTextTyping(true))
                {
                    Debug.LogWarning("[TextTypingEquipment][WARN] No equipment available, using base fallback color=1 eyes=11 outfit=18");
                    ApplyEquipment(DefaultColorItemId, DefaultEyesItemId, DefaultOutfitItemId);
                }
            }
            else
            {
                ApplyEquipment(DefaultColorItemId, DefaultEyesItemId, DefaultOutfitItemId);
            }
            return;
        }

        int colorId = response.data.id_item_color;
        int eyesId = response.data.id_item_cara;
        int outfitId = response.data.id_item_outfit;
        Debug.Log($"[PlayerCustomization] Equipment from backend color={colorId} eyes={eyesId} outfit={outfitId}");
        if (isTextTyping)
        {
            Debug.Log($"[TextTypingEquipment] Backend equipment color={colorId} eyes={eyesId} outfit={outfitId}");
            Debug.Log($"[TextTypingEquipment] Applying equipment to PlayerCustomizationManager color={colorId} eyes={eyesId} outfit={outfitId}");
        }

        ApplyEquipment(colorId, eyesId, outfitId);

        if (isTextTyping)
        {
            Debug.Log($"[TextTypingEquipment] Applying equipment to PlayerVisual color={selectedFaceColorItemId} eyes={selectedEyesItemId} outfit={selectedOutfitItemId}");
        }
    }

    private bool TryApplySessionEquipmentForTextTyping(bool isTextTyping)
    {
        if (!isTextTyping)
        {
            return false;
        }

        int color = TextTypingSession.EquippedColorItemId;
        int eyes = TextTypingSession.EquippedEyesItemId;
        int outfit = TextTypingSession.EquippedOutfitItemId;
        bool hasSessionEquipment = color > 0 && eyes > 0 && outfit > 0;
        if (!hasSessionEquipment)
        {
            return false;
        }

        Debug.Log($"[TextTypingEquipment] Applying equipment to PlayerCustomizationManager color={color} eyes={eyes} outfit={outfit}");
        ApplyEquipment(color, eyes, outfit);
        Debug.Log($"[TextTypingEquipment] Applying equipment to PlayerVisual color={color} eyes={eyes} outfit={outfit}");
        return true;
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
            player.ApplyEquipment(selectedFaceColorItemId, selectedEyesItemId, selectedOutfitItemId);
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