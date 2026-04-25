using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Somnia.Economy.Core;
using Somnia.Economy.Interfaces;
using Somnia.UnityClient;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Somnia.Inventory
{
    [RequireComponent(typeof(UIDocument))]
    public class CosmeticInventoryController : MonoBehaviour
    {
        [Header("Backend")]
        [SerializeField] private int[] catalogIslandIds = { 1, 2 };
        [SerializeField] private CosmeticInventoryDefaults defaults = new();

        [Header("Visual")]
        [SerializeField] private string homeSceneName = "Pantalla_Principal";
        [SerializeField] private CosmeticCategory initialCategory = CosmeticCategory.Color;

        private UIDocument _document;
        private Label _statusLabel;
        private VisualElement _loadingOverlay;
        private Button _tabColor;
        private Button _tabOjos;
        private Button _tabOutfit;
        private VisualElement _newColorIndicator;
        private VisualElement _newOjosIndicator;
        private VisualElement _newOutfitIndicator;
        private Button _configButton;
        private Button _homeButton;
        private Button _closeButton;
        private Button _defaultPreview;
        private VisualElement _defaultVisualSlot;
        private string _activeRootName = "Unknown";

        private readonly Dictionary<int, CosmeticUiBinding> _uiBindings = new();
        private readonly Dictionary<Button, EventCallback<ClickEvent>> _itemButtonHandlers = new();
        private readonly Dictionary<Button, int> _displayedItemByButton = new();
        private readonly Dictionary<int, string> _visualClassByItemId = new();
        private readonly Dictionary<CosmeticCategory, string> _defaultPreviewClassByCategory = new();
        private readonly Dictionary<CosmeticCategory, List<string>> _categoryVisualClassPool = new();
        private readonly Dictionary<int, VisualElement> _defaultVisualVariants = new();

        private CosmeticInventoryService _inventoryService;
        private CosmeticInventorySnapshot _snapshot;
        private CosmeticCategory _activeCategory = CosmeticCategory.Color;
        private CancellationTokenSource _cts;

        private EventCallback<ClickEvent> _onTabColor;
        private EventCallback<ClickEvent> _onTabOjos;
        private EventCallback<ClickEvent> _onTabOutfit;
        private EventCallback<ClickEvent> _onConfig;
        private EventCallback<ClickEvent> _onHome;
        private EventCallback<ClickEvent> _onClose;
        private EventCallback<ClickEvent> _onDefaultPressed;

        private const string EquippedClass = "inventory-equipped";

        private sealed class ScreenBindingDefinition
        {
            public int itemId;
            public string visualClassName;
            public string itemElementName;
            public string lockElementName;
            public string newElementName;
        }

        private sealed class ScreenConfig
        {
            public CosmeticCategory category;
            public string rootName;
            public string defaultButtonName;
            public string defaultVisualClass;
            public string newIndicatorClass;
            public IReadOnlyDictionary<int, string> visualMap;
            public IReadOnlyList<ScreenBindingDefinition> bindings;
        }

        private sealed class CosmeticUiBinding
        {
            public int originalItemId;
            public int displayedItemId;
            public CosmeticCategory category;
            public string visualClassName;
            public string itemElementName;
            public string lockElementName;
            public string newElementName;
            public Button button;
            public VisualElement visualSlot;
            public Dictionary<int, VisualElement> visualVariants = new();
            public VisualElement lockOverlay;
            public VisualElement newIndicator;
        }

        private void OnEnable()
        {
            _document = GetComponent<UIDocument>();
            _activeCategory = ResolveInitialCategory();

            BindUi();
            InitializeService();

            _ = LoadAsync();
        }

        private CosmeticCategory ResolveInitialCategory()
        {
            string sceneName = SceneManager.GetActiveScene().name;

            if (string.Equals(sceneName, "InventarioOjos", StringComparison.OrdinalIgnoreCase))
            {
                return CosmeticCategory.Ojos;
            }

            if (string.Equals(sceneName, "InventarioOutfit", StringComparison.OrdinalIgnoreCase))
            {
                return CosmeticCategory.Outfit;
            }

            return initialCategory;
        }

        private void OnDisable()
        {
            UnbindUiCallbacks();

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private void BindUi()
        {
            var root = _document.rootVisualElement;
            string sceneName = SceneManager.GetActiveScene().name;

            _statusLabel = root.Q<Label>("inventoryStatusLabel");
            _loadingOverlay = root.Q<VisualElement>("inventoryLoadingOverlay");

            _tabColor = QueryFirst<Button>(root, "botoncara", "botonCara");
            _tabOjos = QueryFirst<Button>(root, "botonojos", "botonOjos");
            _tabOutfit = QueryFirst<Button>(root, "botonoutfit", "botonOutfit");
            _newColorIndicator = FindFirstExisting(root, "nuevoCara", "Caranueva", "nuevaCara", "CaraNueva");
            _newOjosIndicator = FindFirstExisting(root, "nuevoOjos", "Ojosnuevo", "ojosNuevo", "nuevoOjo");
            _newOutfitIndicator = FindFirstExisting(root, "nuevoOutfit", "Outfitnuevo", "outfitNuevo", "nuevoTraje");

            SetNewIndicator(_newColorIndicator, false);
            SetNewIndicator(_newOjosIndicator, false);
            SetNewIndicator(_newOutfitIndicator, false);

            Debug.Log($"[NewIndicatorBind] color found={(_newColorIndicator != null ? _newColorIndicator.name : "null")} initialHidden=true");
            Debug.Log($"[NewIndicatorBind] ojos found={(_newOjosIndicator != null ? _newOjosIndicator.name : "null")} initialHidden=true");
            Debug.Log($"[NewIndicatorBind] outfit found={(_newOutfitIndicator != null ? _newOutfitIndicator.name : "null")} initialHidden=true");

            _configButton = QueryFirst<Button>(root, "botonconf", "botonConf", "botonConfig");
            _homeButton = root.Q<Button>("botonHome");
            _closeButton = root.Q<Button>("cerrar");

            _onTabColor = evt =>
            {
                SceneManager.LoadScene("Inventario");
            };

            _onTabOjos = evt =>
            {
                SceneManager.LoadScene("InventarioOjos");
            };

            _onTabOutfit = evt =>
            {
                SceneManager.LoadScene("InventarioOutfit");
            };

            _onConfig = evt =>
            {
                SceneManager.LoadScene("InventarioConfiguracion");
            };

            _onHome = evt =>
            {
                SceneManager.LoadScene(homeSceneName);
            };

            _onClose = evt =>
            {
                SceneManager.LoadScene(homeSceneName);
            };

            _onDefaultPressed = evt =>
            {
                _ = HandleDefaultPressedAsync();
            };

            RegisterButtonCallback(_tabColor, _onTabColor);
            RegisterButtonCallback(_tabOjos, _onTabOjos);
            RegisterButtonCallback(_tabOutfit, _onTabOutfit);

            RegisterButtonCallback(_configButton, _onConfig);
            RegisterButtonCallback(_homeButton, _onHome);
            RegisterButtonCallback(_closeButton, _onClose);

            BuildBindings(root);

            RegisterButtonCallback(_defaultPreview, _onDefaultPressed);
            Debug.Log($"[InventoryScreen] scene={sceneName} category={_activeCategory} root={_activeRootName}");
        }

        private void InitializeService()
        {
            IGameDataService gameDataService = EconomyModule.Instance?.GameDataService;

            if (gameDataService == null)
            {
                var module = FindFirstObjectByType<EconomyModule>();
                gameDataService = module?.GameDataService;
            }

            if (gameDataService == null)
            {
                throw new InvalidOperationException(
                    "No se encontró EconomyModule.GameDataService para cargar inventario cosmético."
                );
            }

            catalogIslandIds = NormalizeCatalogIslandIds(catalogIslandIds);
            _inventoryService = new CosmeticInventoryService(gameDataService, defaults, catalogIslandIds);
        }

        private static int[] NormalizeCatalogIslandIds(int[] input)
        {
            var normalized = (input ?? Array.Empty<int>())
                .Where(id => id == 1 || id == 2)
                .Distinct()
                .OrderBy(id => id)
                .ToArray();

            if (normalized.Length == 0)
            {
                normalized = new[] { 1, 2 };
            }

            Debug.Log($"[Inventory] catalogIslandIds normalizado => {string.Join(",", normalized)}");
            return normalized;
        }

        private async Task LoadAsync()
        {
            if (_inventoryService == null)
            {
                return;
            }

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            SetLoading(true, "Cargando inventario...");

            try
            {
                int slot = Mathf.Max(1, GameSessionManager.Instance?.CurrentSlotNumber ?? 1);

                Debug.Log($"[Inventory] Slot activo detectado: {slot}");

                _snapshot = await _inventoryService.LoadSnapshotAsync(slot, _cts.Token);

                Debug.Log(
                    $"[Inventory] Snapshot cargado. slot={_snapshot.slotNumber} partida={_snapshot.partidaId} items={_snapshot.items.Count}"
                );

                Debug.Log(
                    $"[Inventory] IDs poseídos: {string.Join(",", _snapshot.items.Where(x => x.owned).Select(x => x.itemId).OrderBy(x => x))}"
                );

                Debug.Log(
                    $"[Inventory] Equipamiento actual color={_snapshot.equippedByCategory[CosmeticCategory.Color]} ojos={_snapshot.equippedByCategory[CosmeticCategory.Ojos]} outfit={_snapshot.equippedByCategory[CosmeticCategory.Outfit]}"
                );

                SetLoading(false, $"Partida #{_snapshot.partidaId} | Slot {slot}");
                RenderCurrentCategory();
                UpdateSideNewIndicators();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Inventory] Error cargando inventario: {ex}");
                SetLoading(false, $"Error: {ex.Message}");
            }
        }

        private void SwitchCategory(CosmeticCategory category)
        {
            _activeCategory = category;

            Debug.Log($"[Inventory] Categoría activa: {_activeCategory}");

            RenderCurrentCategory();
        }

        private void RenderCurrentCategory()
        {
            if (_snapshot?.items == null)
            {
                return;
            }

            UpdateTabClasses();

            int equippedItemId = GetEquippedIdForCategory(_activeCategory);
            int defaultItemId = defaults.GetDefaultId(_activeCategory);

            _displayedItemByButton.Clear();
            ApplyDefaultPreviewVisual(_activeCategory, equippedItemId);
            if (_defaultPreview != null)
            {
                _displayedItemByButton[_defaultPreview] = equippedItemId;
                Debug.Log(
                    $"[InventorySwap] category={_activeCategory} button={_defaultPreview.name} original={defaultItemId} displayed={equippedItemId}"
                );
            }

            foreach (var binding in _uiBindings.Values.Where(x => x.category == _activeCategory))
            {
                int displayedItemId = binding.originalItemId;

                if (binding.originalItemId == equippedItemId)
                {
                    displayedItemId = defaultItemId;
                }

                binding.displayedItemId = displayedItemId;

                var displayedItem = FindItem(displayedItemId, binding.category);
                bool owned = displayedItem?.owned ?? false;
                bool equipped = displayedItemId == equippedItemId;
                bool isNew = displayedItem?.isNew ?? false;

                ApplyLockState(binding, owned);
                ApplyItemVisual(binding, displayedItemId);
                ApplyNewState(binding, isNew);

                if (binding.button != null)
                {
                    binding.button.EnableInClassList(EquippedClass, equipped);
                    _displayedItemByButton[binding.button] = displayedItemId;
                }

                Debug.Log(
                    $"[InventorySwap] category={_activeCategory} button={binding.itemElementName} original={binding.originalItemId} displayed={displayedItemId}"
                );
            }

            if (_defaultPreview != null)
            {
                _defaultPreview.style.display = DisplayStyle.Flex;
                _defaultPreview.EnableInClassList(EquippedClass, true);
            }
        }

        private void ApplyDefaultPreviewVisual(CosmeticCategory category, int equippedItemId)
        {
            if (_defaultPreview == null)
            {
                return;
            }

            int fallbackDefaultId = defaults.GetDefaultId(category);
            int visualItemId = equippedItemId > 0 ? equippedItemId : fallbackDefaultId;

            if (ShouldUseVisualSlot(category))
            {
                ApplyOnlyOneVisual(_defaultPreview, _defaultVisualSlot, _defaultVisualVariants, category, visualItemId);
                return;
            }

            if (_visualClassByItemId.TryGetValue(visualItemId, out var equippedVisualClass))
            {
                ApplyVisualClassForCategory(GetVisualTarget(_defaultPreview, _defaultVisualSlot, category), category, equippedVisualClass);
                return;
            }

            if (_defaultPreviewClassByCategory.TryGetValue(category, out var defaultPreviewClass))
            {
                ApplyVisualClassForCategory(GetVisualTarget(_defaultPreview, _defaultVisualSlot, category), category, defaultPreviewClass);
            }
        }

        private void ApplyItemVisual(CosmeticUiBinding binding, int visualItemId)
        {
            if (binding?.button == null)
            {
                return;
            }

            if (!_visualClassByItemId.TryGetValue(visualItemId, out var visualClass))
            {
                visualClass = binding.visualClassName;
            }

            if (ShouldUseVisualSlot(binding.category))
            {
                ApplyOnlyOneVisual(binding.button, binding.visualSlot, binding.visualVariants, binding.category, visualItemId);
                return;
            }

            ApplyVisualClassForCategory(GetVisualTarget(binding.button, binding.visualSlot, binding.category), binding.category, visualClass);
        }

        private void ApplyOnlyOneVisual(
            Button button,
            VisualElement slot,
            Dictionary<int, VisualElement> visualVariants,
            CosmeticCategory category,
            int displayedItemId
        )
        {
            if (button == null || slot == null)
            {
                return;
            }

            EnsureVisualVariants(button, slot, visualVariants, category);

            string activeVisual = "none";
            List<string> hiddenVisuals = new();

            foreach (var pair in visualVariants.OrderBy(x => x.Key))
            {
                bool isActive = pair.Key == displayedItemId;
                ApplyVisualAlignmentFix(pair.Value, category, button, isActive);
                pair.Value.style.display = isActive ? DisplayStyle.Flex : DisplayStyle.None;
                pair.Value.style.visibility = isActive ? Visibility.Visible : Visibility.Hidden;

                string visualName = GetVisualLogName(category, pair.Key);
                if (isActive)
                {
                    activeVisual = visualName;
                }
                else
                {
                    hiddenVisuals.Add(visualName);
                }
            }

            Debug.Log(
                $"[VisualSlot] button={button.name} displayed={displayedItemId} activeVisual={activeVisual} hidden={string.Join(",", hiddenVisuals)}"
            );
        }

        private void ApplyVisualAlignmentFix(
            VisualElement visual,
            CosmeticCategory category,
            Button button,
            bool isActive
        )
        {
            if (visual == null)
            {
                return;
            }

            // Reset de offsets acumulados: solo tocamos el visual hijo, nunca el botón padre.
            visual.style.left = 0;
            visual.style.top = 0;
            visual.style.right = 0;
            visual.style.bottom = 0;
            visual.style.marginLeft = 0;
            visual.style.marginRight = 0;
            visual.style.marginTop = 0;
            visual.style.marginBottom = 0;
            visual.style.paddingLeft = 0;
            visual.style.paddingRight = 0;
            visual.style.paddingTop = 0;
            visual.style.paddingBottom = 0;
            visual.style.alignSelf = Align.Center;
            visual.style.justifyContent = Justify.Center;
            visual.style.alignItems = Align.Center;
            visual.style.translate = new Translate(
                new Length(0f, LengthUnit.Pixel),
                new Length(0f, LengthUnit.Pixel),
                0f
            );
            visual.transform.position = Vector3.zero;
            visual.transform.scale = Vector3.one;
            visual.transform.rotation = Quaternion.identity;

            if (!isActive)
            {
                return;
            }

            // Ajuste fino solicitado: en outfit de lista, centrar ligeramente hacia la izquierda.
            if (category == CosmeticCategory.Outfit && button != null && button != _defaultPreview)
            {
                visual.style.translate = new Translate(
                    new Length(-8f, LengthUnit.Pixel),
                    new Length(0f, LengthUnit.Pixel),
                    0f
                );
            }
        }

        private void EnsureVisualVariants(
            Button button,
            VisualElement slot,
            Dictionary<int, VisualElement> visualVariants,
            CosmeticCategory category
        )
        {
            if (button == null || slot == null || visualVariants == null)
            {
                return;
            }

            button.style.backgroundImage = StyleKeyword.None;

            foreach (var pair in _visualClassByItemId.OrderBy(x => x.Key))
            {
                if (visualVariants.ContainsKey(pair.Key))
                {
                    continue;
                }

                string visualName = $"visual-{button.name}-{pair.Key}";
                var visual = slot.Q<VisualElement>(visualName);
                if (visual == null)
                {
                    visual = new VisualElement { name = visualName };
                    slot.Insert(slot.childCount, visual);
                }

                visual.pickingMode = PickingMode.Ignore;
                visual.style.position = Position.Absolute;
                visual.style.left = 0;
                visual.style.top = 0;
                visual.style.right = 0;
                visual.style.bottom = 0;
                visual.style.display = DisplayStyle.None;
                visual.style.visibility = Visibility.Hidden;

                ApplyVisualClassForCategory(visual, category, pair.Value);
                visualVariants[pair.Key] = visual;
            }
        }

        private static string GetVisualLogName(CosmeticCategory category, int itemId)
        {
            return category switch
            {
                CosmeticCategory.Ojos => itemId switch
                {
                    11 => "ovalos",
                    12 => "rombos",
                    13 => "cansado",
                    14 => "estrella",
                    15 => "happy",
                    16 => "pirata",
                    17 => "enojado",
                    _ => $"item{itemId}"
                },
                CosmeticCategory.Outfit => itemId switch
                {
                    18 => "boyScout",
                    19 => "engrane",
                    20 => "rana",
                    21 => "diablito",
                    _ => $"item{itemId}"
                },
                _ => $"item{itemId}"
            };
        }

        private static bool ShouldUseVisualSlot(CosmeticCategory category)
        {
            return category == CosmeticCategory.Ojos || category == CosmeticCategory.Outfit;
        }

        private static VisualElement GetVisualTarget(Button button, VisualElement visualSlot, CosmeticCategory category)
        {
            return ShouldUseVisualSlot(category) ? visualSlot ?? button : button;
        }

        private static VisualElement EnsureVisualSlot(Button button, string slotName, CosmeticCategory category)
        {
            if (button == null || !ShouldUseVisualSlot(category))
            {
                return null;
            }

            var slot = button.Q<VisualElement>(slotName);
            if (slot == null)
            {
                string categorySlotClass = GetSlotClassForCategory(category);
                if (!string.IsNullOrWhiteSpace(categorySlotClass))
                {
                    slot = button.Q<VisualElement>(className: categorySlotClass);
                    if (slot != null && string.IsNullOrWhiteSpace(slot.name))
                    {
                        slot.name = slotName;
                    }
                }
            }

            if (slot == null)
            {
                slot = new VisualElement { name = slotName };
                button.Insert(0, slot);
            }

            string slotClass = GetSlotClassForCategory(category);
            if (!string.IsNullOrWhiteSpace(slotClass) && !slot.ClassListContains(slotClass))
            {
                slot.AddToClassList(slotClass);
            }

            slot.pickingMode = PickingMode.Ignore;
            slot.style.position = Position.Absolute;
            slot.style.left = 0;
            slot.style.top = 0;
            slot.style.right = 0;
            slot.style.bottom = 0;
            slot.style.marginLeft = 0;
            slot.style.marginRight = 0;
            slot.style.marginTop = 0;
            slot.style.marginBottom = 0;
            slot.style.paddingLeft = 0;
            slot.style.paddingRight = 0;
            slot.style.paddingTop = 0;
            slot.style.paddingBottom = 0;

            return slot;
        }

        private static string GetSlotClassForCategory(CosmeticCategory category)
        {
            return category switch
            {
                CosmeticCategory.Ojos => "eyesVisualSlot",
                CosmeticCategory.Outfit => "outfitVisualSlot",
                _ => "inventoryVisualSlot"
            };
        }

        private void ApplyVisualClassForCategory(VisualElement element, CosmeticCategory category, string visualClass)
        {
            if (element == null || string.IsNullOrWhiteSpace(visualClass))
            {
                return;
            }

            if (_categoryVisualClassPool.TryGetValue(category, out var classPool))
            {
                foreach (var className in classPool)
                {
                    element.RemoveFromClassList(className);
                }
            }

            element.AddToClassList(visualClass);
        }

        private void ApplyLockState(CosmeticUiBinding binding, bool owned)
        {
            if (binding == null || binding.lockOverlay == null)
            {
                return;
            }

            binding.lockOverlay.pickingMode = PickingMode.Ignore;

            string removedClasses = string.Empty;
            string addedClasses = string.Empty;

            if (owned)
            {
                binding.lockOverlay.style.display = DisplayStyle.None;
                binding.lockOverlay.style.visibility = Visibility.Hidden;

                removedClasses = RemoveLockClasses(binding.lockOverlay);
            }
            else
            {
                binding.lockOverlay.style.display = DisplayStyle.Flex;
                binding.lockOverlay.style.visibility = Visibility.Visible;

                addedClasses = AddLockClassForCategory(binding.lockOverlay, binding.category);
            }

            Debug.Log(
                $"[Inventory][LockState] originalItemId={binding.originalItemId} displayedItemId={binding.displayedItemId} owned={owned} button={binding.itemElementName} lockOverlay={binding.lockElementName} display={binding.lockOverlay.style.display.value} visibility={binding.lockOverlay.style.visibility.value} removedClasses={removedClasses} addedClasses={addedClasses}"
            );
        }

        private void ApplyNewState(CosmeticUiBinding binding, bool itemNuevo)
        {
            if (binding?.newIndicator == null)
            {
                return;
            }

            binding.newIndicator.pickingMode = PickingMode.Ignore;
            string newClass = GetNewIndicatorClass(binding.category);
            if (!string.IsNullOrWhiteSpace(newClass) && !binding.newIndicator.ClassListContains(newClass))
            {
                binding.newIndicator.AddToClassList(newClass);
            }

            if (itemNuevo)
            {
                binding.newIndicator.style.display = DisplayStyle.Flex;
                binding.newIndicator.style.visibility = Visibility.Visible;
            }
            else
            {
                binding.newIndicator.style.display = DisplayStyle.None;
                binding.newIndicator.style.visibility = Visibility.Hidden;
            }
        }

        private void UpdateSideNewIndicators()
        {
            bool hasNewColor = _snapshot?.hasNewByCategory != null &&
                               _snapshot.hasNewByCategory.TryGetValue(CosmeticCategory.Color, out bool colorFromSummary)
                ? colorFromSummary
                : _snapshot?.items?.Any(i => i.category == CosmeticCategory.Color && i.isNew) == true;

            bool hasNewOjos = _snapshot?.hasNewByCategory != null &&
                              _snapshot.hasNewByCategory.TryGetValue(CosmeticCategory.Ojos, out bool ojosFromSummary)
                ? ojosFromSummary
                : _snapshot?.items?.Any(i => i.category == CosmeticCategory.Ojos && i.isNew) == true;

            bool hasNewOutfit = _snapshot?.hasNewByCategory != null &&
                                _snapshot.hasNewByCategory.TryGetValue(CosmeticCategory.Outfit, out bool outfitFromSummary)
                ? outfitFromSummary
                : _snapshot?.items?.Any(i => i.category == CosmeticCategory.Outfit && i.isNew) == true;

            Debug.Log($"[NewSummaryRaw] color={hasNewColor} ojos={hasNewOjos} outfit={hasNewOutfit}");

            SetNewIndicator(_newColorIndicator, hasNewColor);
            SetNewIndicator(_newOjosIndicator, hasNewOjos);
            SetNewIndicator(_newOutfitIndicator, hasNewOutfit);

            LogIndicatorApply("color", _newColorIndicator, hasNewColor);
            LogIndicatorApply("ojos", _newOjosIndicator, hasNewOjos);
            LogIndicatorApply("outfit", _newOutfitIndicator, hasNewOutfit);
        }

        private static string GetNewIndicatorClass(CosmeticCategory category)
        {
            return category switch
            {
                CosmeticCategory.Ojos => "nuevaCompra",
                CosmeticCategory.Outfit => "nuevaCompra",
                _ => "nuevoInventario"
            };
        }


        private static void SetNewIndicator(VisualElement indicator, bool visible)
        {
            if (indicator == null)
            {
                return;
            }

            indicator.pickingMode = PickingMode.Ignore;
            indicator.SetEnabled(true);

            indicator.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            indicator.style.visibility = visible ? Visibility.Visible : Visibility.Hidden;
            indicator.visible = visible;

            indicator.EnableInClassList("nuevoInventario", visible);
            indicator.EnableInClassList("nuevaCompra", visible);
            indicator.EnableInClassList("nuevoAlerta", visible);
            indicator.EnableInClassList("nuevoAlert", visible);
        }

        private static void LogIndicatorApply(string categoryLabel, VisualElement indicator, bool visible)
        {
            string displayValue = indicator?.style.display.value switch
            {
                DisplayStyle.Flex => "Flex",
                DisplayStyle.None => "None",
                _ => indicator?.style.display.value.ToString() ?? "Unknown"
            };

            string visibilityValue = indicator?.style.visibility.value switch
            {
                Visibility.Visible => "Visible",
                Visibility.Hidden => "Hidden",
                _ => indicator?.style.visibility.value.ToString() ?? "Unknown"
            };

            Debug.Log($"[NewIndicatorApply] {categoryLabel} visible={visible} display={displayValue} visibility={visibilityValue}");
        }

        private static string RemoveLockClasses(VisualElement lockOverlay)
        {
            List<string> removed = new();

            if (lockOverlay.ClassListContains("bloqueado"))
            {
                lockOverlay.RemoveFromClassList("bloqueado");
                removed.Add("bloqueado");
            }

            if (lockOverlay.ClassListContains("bloqueadoOjos"))
            {
                lockOverlay.RemoveFromClassList("bloqueadoOjos");
                removed.Add("bloqueadoOjos");
            }

            if (lockOverlay.ClassListContains("bloqueadoOutfit"))
            {
                lockOverlay.RemoveFromClassList("bloqueadoOutfit");
                removed.Add("bloqueadoOutfit");
            }

            return removed.Count == 0 ? "none" : string.Join(",", removed);
        }

        private static string AddLockClassForCategory(VisualElement lockOverlay, CosmeticCategory category)
        {
            RemoveLockClasses(lockOverlay);
            lockOverlay.AddToClassList("bloqueado");
            return "bloqueado";
        }

        private async Task HandleDefaultPressedAsync()
        {
            if (_snapshot?.items == null)
            {
                SetStatus("Inventario todavía no cargado.");
                return;
            }

            int defaultItemId = defaults.GetDefaultId(_activeCategory);
            if (_displayedItemByButton.TryGetValue(_defaultPreview, out var displayedDefaultId))
            {
                defaultItemId = displayedDefaultId;
            }

            var item = _snapshot.items.FirstOrDefault(i =>
                i.itemId == defaultItemId && i.category == _activeCategory
            );

            if (item == null)
            {
                Debug.LogWarning(
                    $"[Inventory] Default no encontrado para {_activeCategory}. itemId={defaultItemId}"
                );

                SetStatus($"Default no encontrado para {_activeCategory}.");
                return;
            }

            Debug.Log(
                $"[Inventory] Botón predeterminado presionado para {_activeCategory}. item={item.itemId}"
            );

            await TryEquipItemAsync(item);
        }

        private async Task HandleItemPressedAsync(int itemId)
        {
            Debug.Log($"[Inventory] Botón presionado: item={itemId}");

            if (_snapshot?.items == null)
            {
                SetStatus("Inventario todavía no cargado.");
                return;
            }

            var item = _snapshot.items.FirstOrDefault(i => i.itemId == itemId);

            if (item == null)
            {
                SetStatus($"Item {itemId} no encontrado en snapshot.");
                return;
            }

            await TryEquipItemAsync(item);
        }

        private async Task TryEquipItemAsync(CosmeticInventoryItemViewModel item)
        {
            if (item == null)
            {
                return;
            }

            if (!item.owned)
            {
                SetStatus("Ítem bloqueado: compra requerida en tienda.");
                return;
            }

            if (item.equipped)
            {
                SetStatus($"{item.name} ya está equipado.");
                return;
            }

            await EquipItemAsync(item);
        }

        private async Task EquipItemAsync(CosmeticInventoryItemViewModel item)
        {
            if (_inventoryService == null || _snapshot == null)
            {
                SetStatus("No se pudo equipar: inventario no inicializado.");
                return;
            }

            Debug.Log(
                $"[Inventory] Intento de equipar item={item.itemId} category={item.category} slot={_snapshot.slotNumber}"
            );

            SetStatus($"Equipando {item.name}...");

            try
            {
                var response = await _inventoryService.EquipAsync(
                    _snapshot.slotNumber,
                    _snapshot,
                    item.category,
                    item.itemId,
                    _cts.Token
                );

                if (!response.success)
                {
                    SetStatus($"No se pudo equipar: {response.message}");
                    Debug.LogWarning(
                        $"[Inventory] Equip failed item={item.itemId} reason={response.message}"
                    );
                    return;
                }

                var equippedData = response.data;

                Debug.Log(
                    $"[Inventory] Backend equip response color={equippedData.id_item_color} ojos={equippedData.id_item_cara} outfit={equippedData.id_item_outfit}"
                );

                _snapshot.equippedByCategory[CosmeticCategory.Ojos] = equippedData.id_item_cara;
                _snapshot.equippedByCategory[CosmeticCategory.Color] = equippedData.id_item_color;
                _snapshot.equippedByCategory[CosmeticCategory.Outfit] = equippedData.id_item_outfit;

                foreach (var vm in _snapshot.items)
                {
                    vm.equipped =
                        _snapshot.equippedByCategory.TryGetValue(vm.category, out var equippedId)
                        && equippedId == vm.itemId;
                }

                var equippedItem = _snapshot.items.FirstOrDefault(vm => vm.itemId == item.itemId && vm.category == item.category);
                if (equippedItem != null)
                {
                    equippedItem.isNew = false;
                }

                if (_snapshot.hasNewByCategory != null)
                {
                    _snapshot.hasNewByCategory[item.category] =
                        _snapshot.items.Any(vm => vm.category == item.category && vm.isNew);
                }

                SetStatus($"Equipado: {item.name}");
                RenderCurrentCategory();
                UpdateSideNewIndicators();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Inventory] Error equipando item {item.itemId}: {ex}");
                SetStatus($"Error equipando: {ex.Message}");
            }
        }

        private void SetLoading(bool loading, string message)
        {
            if (_loadingOverlay != null)
            {
                _loadingOverlay.style.display = loading ? DisplayStyle.Flex : DisplayStyle.None;
                _loadingOverlay.pickingMode = loading ? PickingMode.Position : PickingMode.Ignore;
            }

            SetStatus(message);
        }

        private void SetStatus(string message)
        {
            if (_statusLabel != null)
            {
                _statusLabel.text = message;
            }
        }

        private int GetEquippedIdForCategory(CosmeticCategory category)
        {
            if (_snapshot?.equippedByCategory == null)
            {
                return defaults.GetDefaultId(category);
            }

            return _snapshot.equippedByCategory.TryGetValue(category, out var equippedId)
                ? equippedId
                : defaults.GetDefaultId(category);
        }

        private CosmeticInventoryItemViewModel FindItem(int itemId, CosmeticCategory category)
        {
            if (_snapshot?.items == null)
            {
                return null;
            }

            return _snapshot.items.FirstOrDefault(i => i.itemId == itemId && i.category == category);
        }

        private void BuildBindings(VisualElement root)
        {
            _uiBindings.Clear();
            _itemButtonHandlers.Clear();
            _displayedItemByButton.Clear();
            _visualClassByItemId.Clear();
            _defaultPreviewClassByCategory.Clear();
            _categoryVisualClassPool.Clear();
            _defaultVisualVariants.Clear();

            ScreenConfig config = GetScreenConfig(_activeCategory);
            _activeRootName = config.rootName;
            _defaultPreview = root.Q<Button>(config.defaultButtonName);
            _defaultVisualSlot = EnsureVisualSlot(_defaultPreview, $"slot-{config.defaultButtonName}", config.category);

            RegisterCategoryVisuals(config.category, config.defaultVisualClass, config.visualMap);

            foreach (var definition in config.bindings)
            {
                AddBinding(
                    root,
                    definition.itemId,
                    config.category,
                    definition.visualClassName,
                    definition.itemElementName,
                    definition.lockElementName,
                    definition.newElementName
                );
            }

            Debug.Log($"[BuildBindings] category={config.category} default={config.defaultButtonName}");
        }

        private ScreenConfig GetScreenConfig(CosmeticCategory category)
        {
            return category switch
            {
                CosmeticCategory.Ojos => BuildOjosConfig(),
                CosmeticCategory.Outfit => BuildOutfitConfig(),
                _ => BuildColorConfig()
            };
        }

        private ScreenConfig BuildColorConfig()
        {
            return new ScreenConfig
            {
                category = CosmeticCategory.Color,
                rootName = "Inventario.uxml",
                defaultButtonName = "predeterminado",
                defaultVisualClass = "inv-color-blanco",
                newIndicatorClass = "nuevoInventario",
                visualMap = new Dictionary<int, string>
                {
                    { defaults.defaultColorItemId, "inv-color-blanco" },
                    { 2, "inv-color-naranja" },
                    { 3, "inv-color-morado" },
                    { 4, "inv-color-amarillo" },
                    { 5, "inv-color-rojo" },
                    { 6, "inv-color-turquesa" },
                    { 7, "inv-color-verde" },
                    { 8, "inv-color-rosa" },
                    { 9, "inv-color-azul" },
                    { 10, "inv-color-negro" }
                },
                bindings = new[]
                {
                    new ScreenBindingDefinition { itemId = 2, visualClassName = "inv-color-naranja", itemElementName = "skinNaranja", lockElementName = "bloqueado1", newElementName = "naranjaNuevo1" },
                    new ScreenBindingDefinition { itemId = 3, visualClassName = "inv-color-morado", itemElementName = "skinMorado", lockElementName = "bloqueado2", newElementName = "moradoNuevo2" },
                    new ScreenBindingDefinition { itemId = 4, visualClassName = "inv-color-amarillo", itemElementName = "skinAmarillo", lockElementName = "bloqueado3", newElementName = "amarilloNuevo3" },
                    new ScreenBindingDefinition { itemId = 5, visualClassName = "inv-color-rojo", itemElementName = "skinRojo", lockElementName = "bloqueado4", newElementName = "rojoNuevo4" },
                    new ScreenBindingDefinition { itemId = 6, visualClassName = "inv-color-turquesa", itemElementName = "skinTurquesa", lockElementName = "bloqueado5", newElementName = "turquesaNuevo5" },
                    new ScreenBindingDefinition { itemId = 7, visualClassName = "inv-color-verde", itemElementName = "skinVerde", lockElementName = "bloqueado6", newElementName = "verdeNuevo6" },
                    new ScreenBindingDefinition { itemId = 8, visualClassName = "inv-color-rosa", itemElementName = "skinRosa", lockElementName = "bloqueado7", newElementName = "rosaNuevo7" },
                    new ScreenBindingDefinition { itemId = 9, visualClassName = "inv-color-azul", itemElementName = "skinAzul", lockElementName = "bloqueado8", newElementName = "azulNuevo8" },
                    new ScreenBindingDefinition { itemId = 10, visualClassName = "inv-color-negro", itemElementName = "skinNegro", lockElementName = "bloqueado9", newElementName = "negroNuevo9" }
                }
            };
        }

        private ScreenConfig BuildOjosConfig()
        {
            return new ScreenConfig
            {
                category = CosmeticCategory.Ojos,
                rootName = "OjosInvent.uxml",
                defaultButtonName = "ojosPredeterminado",
                defaultVisualClass = "ojospred",
                newIndicatorClass = "nuevaCompra",
                visualMap = new Dictionary<int, string>
                {
                    { defaults.defaultOjosItemId, "ojospred" },
                    { 12, "ojos1" },
                    { 13, "ojos2" },
                    { 14, "ojos3" },
                    { 15, "ojos4" },
                    { 16, "ojos5" },
                    { 17, "ojos6" }
                },
                bindings = new[]
                {
                    new ScreenBindingDefinition { itemId = 12, visualClassName = "ojos1", itemElementName = "ojosRombo", lockElementName = "bloqueadoojos1", newElementName = "romboNuevo1" },
                    new ScreenBindingDefinition { itemId = 13, visualClassName = "ojos2", itemElementName = "ojosCansados", lockElementName = "bloqueadoojos2", newElementName = "cansadosNuevo2" },
                    new ScreenBindingDefinition { itemId = 14, visualClassName = "ojos3", itemElementName = "ojosEstrella", lockElementName = "bloqueadoojos3", newElementName = "estrellaNuevo3" },
                    new ScreenBindingDefinition { itemId = 15, visualClassName = "ojos4", itemElementName = "ojosHappy", lockElementName = "bloqueadoojos4", newElementName = "happyNuevo4" },
                    new ScreenBindingDefinition { itemId = 16, visualClassName = "ojos5", itemElementName = "ojosPirata", lockElementName = "bloqueadoojos5", newElementName = "pirataNuevo5" },
                    new ScreenBindingDefinition { itemId = 17, visualClassName = "ojos6", itemElementName = "ojosEnojado", lockElementName = "bloqueadoojos6", newElementName = "enojadoNuevo6" }
                }
            };
        }

        private ScreenConfig BuildOutfitConfig()
        {
            return new ScreenConfig
            {
                category = CosmeticCategory.Outfit,
                rootName = "OutfitInventario.uxml",
                defaultButtonName = "outfitPredeterminado",
                defaultVisualClass = "outfitPredterminado",
                newIndicatorClass = "nuevaCompra",
                visualMap = new Dictionary<int, string>
                {
                    { defaults.defaultOutfitItemId, "outfitPredterminado" },
                    { 19, "outfit3" },
                    { 20, "outfit1" },
                    { 21, "outfit2" }
                },
                bindings = new[]
                {
                    new ScreenBindingDefinition { itemId = 19, visualClassName = "outfit3", itemElementName = "outfitEngrane", lockElementName = "bloqueado3", newElementName = "engraneNuevo3" },
                    new ScreenBindingDefinition { itemId = 20, visualClassName = "outfit1", itemElementName = "outfitRana", lockElementName = "bloqueado1", newElementName = "ranaNuevo1" },
                    new ScreenBindingDefinition { itemId = 21, visualClassName = "outfit2", itemElementName = "outfitDiablo", lockElementName = "bloqueado2", newElementName = "diabloNuevo2" }
                }
            };
        }

        private void RegisterCategoryVisuals(
            CosmeticCategory category,
            string defaultPreviewClass,
            IReadOnlyDictionary<int, string> itemVisualMap
        )
        {
            _defaultPreviewClassByCategory[category] = defaultPreviewClass;

            var classPool = new HashSet<string> { defaultPreviewClass };

            foreach (var pair in itemVisualMap)
            {
                _visualClassByItemId[pair.Key] = pair.Value;
                classPool.Add(pair.Value);
            }

            _categoryVisualClassPool[category] = classPool.ToList();
        }

        private void AddBinding(
            VisualElement root,
            int itemId,
            CosmeticCategory category,
            string visualClassName,
            string itemElementName,
            string lockElementName,
            string newElementName = null
        )
        {
            var button = root.Q<Button>(itemElementName);

            if (button == null)
            {
                Debug.LogWarning(
                    $"[Inventory] No se encontró botón UXML para item {itemId}: {itemElementName}"
                );
                return;
            }

            var lockOverlay = root.Q<VisualElement>(lockElementName);

            if (lockOverlay == null)
            {
                Debug.LogWarning(
                    $"[Inventory] No se encontró lock overlay para item {itemId}: {lockElementName}"
                );
            }
            else
            {
                lockOverlay.pickingMode = PickingMode.Ignore;
            }

            var newIndicator = string.IsNullOrWhiteSpace(newElementName)
                ? null
                : root.Q<VisualElement>(newElementName);

            if (!string.IsNullOrWhiteSpace(newElementName) && newIndicator == null)
            {
                Debug.LogWarning(
                    $"[Inventory] No se encontró indicador de nuevo para item {itemId}: {newElementName}"
                );
            }

            var binding = new CosmeticUiBinding
            {
                originalItemId = itemId,
                displayedItemId = itemId,
                category = category,
                visualClassName = visualClassName,
                itemElementName = itemElementName,
                lockElementName = lockElementName,
                newElementName = newElementName,
                button = button,
                visualSlot = EnsureVisualSlot(button, $"slot-{itemElementName}", category),
                lockOverlay = lockOverlay,
                newIndicator = newIndicator
            };

            _uiBindings[itemId] = binding;

            EventCallback<ClickEvent> handler = evt =>
            {
                int displayedItemId = binding.displayedItemId;
                if (_displayedItemByButton.TryGetValue(button, out var displayedByButton))
                {
                    displayedItemId = displayedByButton;
                }

                _ = HandleItemPressedAsync(displayedItemId);
            };

            _itemButtonHandlers[button] = handler;

            RegisterButtonCallback(button, handler);

            Debug.Log(
                $"[Inventory] Binding registrado item={itemId} category={category} ui={itemElementName} lock={lockElementName}"
            );
        }

        private void UnbindUiCallbacks()
        {
            UnregisterButtonCallback(_tabColor, _onTabColor);
            UnregisterButtonCallback(_tabOjos, _onTabOjos);
            UnregisterButtonCallback(_tabOutfit, _onTabOutfit);

            UnregisterButtonCallback(_configButton, _onConfig);
            UnregisterButtonCallback(_homeButton, _onHome);
            UnregisterButtonCallback(_closeButton, _onClose);

            UnregisterButtonCallback(_defaultPreview, _onDefaultPressed);

            foreach (var pair in _itemButtonHandlers)
            {
                UnregisterButtonCallback(pair.Key, pair.Value);
            }

            _itemButtonHandlers.Clear();
            _displayedItemByButton.Clear();
        }

        private static void RegisterButtonCallback(Button button, EventCallback<ClickEvent> callback)
        {
            if (button == null || callback == null)
            {
                return;
            }

            button.RegisterCallback(callback);
        }

        private static void UnregisterButtonCallback(Button button, EventCallback<ClickEvent> callback)
        {
            if (button == null || callback == null)
            {
                return;
            }

            button.UnregisterCallback(callback);
        }

        private void UpdateTabClasses()
        {
            SetTabState(_tabColor, _activeCategory == CosmeticCategory.Color);
            SetTabState(_tabOjos, _activeCategory == CosmeticCategory.Ojos);
            SetTabState(_tabOutfit, _activeCategory == CosmeticCategory.Outfit);
        }

        private static void SetTabState(VisualElement tab, bool active)
        {
            if (tab == null)
            {
                return;
            }

            if (active)
            {
                tab.AddToClassList("inventory-tab-active");
            }
            else
            {
                tab.RemoveFromClassList("inventory-tab-active");
            }
        }

        private static T QueryFirst<T>(VisualElement root, params string[] names) where T : VisualElement
        {
            if (root == null || names == null)
            {
                return null;
            }

            foreach (var name in names)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var element = root.Q<T>(name);
                if (element != null)
                {
                    return element;
                }
            }

            return null;
        }

        private static VisualElement FindFirstExisting(VisualElement root, params string[] names)
        {
            if (root == null || names == null)
            {
                return null;
            }

            foreach (string name in names)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var element = root.Q<VisualElement>(name);
                if (element != null)
                {
                    return element;
                }
            }

            return null;
        }
    }
}