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

        private readonly Dictionary<int, CosmeticUiBinding> _uiBindings = new();
        private readonly Dictionary<Button, EventCallback<ClickEvent>> _itemButtonHandlers = new();
        private readonly Dictionary<Button, int> _displayedItemByButton = new();
        private readonly Dictionary<int, string> _visualClassByItemId = new();
        private readonly Dictionary<CosmeticCategory, string> _defaultPreviewClassByCategory = new();
        private readonly Dictionary<CosmeticCategory, List<string>> _categoryVisualClassPool = new();

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

            _statusLabel = root.Q<Label>("inventoryStatusLabel");
            _loadingOverlay = root.Q<VisualElement>("inventoryLoadingOverlay");

            _tabColor = root.Q<Button>("botoncara");
            _tabOjos = root.Q<Button>("botonojos");
            _tabOutfit = root.Q<Button>("botonoutfit");
            _newColorIndicator = root.Q<VisualElement>("nuevoCara");
            _newOjosIndicator = root.Q<VisualElement>("nuevoOjos");
            _newOutfitIndicator = root.Q<VisualElement>("nuevoOutfit");

            _configButton = root.Q<Button>("botonconf");
            _homeButton = root.Q<Button>("botonHome");
            _closeButton = root.Q<Button>("cerrar");

            _defaultPreview = root.Q<Button>("predeterminado");

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

            RegisterButtonCallback(_defaultPreview, _onDefaultPressed);

            BuildBindings(root);
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
                UpdateCategoryNewIndicators();
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

            foreach (var binding in _uiBindings.Values)
            {
                bool visible = binding.category == _activeCategory;

                if (binding.button != null)
                {
                    binding.button.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }

            int equippedItemId = GetEquippedIdForCategory(_activeCategory);
            int defaultItemId = defaults.GetDefaultId(_activeCategory);

            _displayedItemByButton.Clear();
            ApplyDefaultPreviewVisual(_activeCategory, equippedItemId);
            if (_defaultPreview != null)
            {
                _displayedItemByButton[_defaultPreview] = equippedItemId;
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

                ApplyLockState(binding, owned);
                ApplyItemVisual(binding, displayedItemId);
                ApplyNewState(binding, displayedItem?.isNew ?? false);

                if (binding.button != null)
                {
                    binding.button.EnableInClassList(EquippedClass, equipped);
                    _displayedItemByButton[binding.button] = displayedItemId;
                }

                string lockDisplay = binding.lockOverlay?.style.display.value.ToString() ?? "none";
                string lockVisibility = binding.lockOverlay?.style.visibility.value.ToString() ?? "none";
                Debug.Log(
                    $"[InventorySwap] button={binding.itemElementName} original={binding.originalItemId} displayed={displayedItemId} owned={owned} equipped={equipped} lockDisplay={lockDisplay} lockVisibility={lockVisibility}"
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

            if (_visualClassByItemId.TryGetValue(visualItemId, out var equippedVisualClass))
            {
                ApplyVisualClassForCategory(_defaultPreview, category, equippedVisualClass);
                return;
            }

            if (_defaultPreviewClassByCategory.TryGetValue(category, out var defaultPreviewClass))
            {
                ApplyVisualClassForCategory(_defaultPreview, category, defaultPreviewClass);
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

            ApplyVisualClassForCategory(binding.button, binding.category, visualClass);
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
            if (!binding.newIndicator.ClassListContains("nuevoInventario"))
            {
                binding.newIndicator.AddToClassList("nuevoInventario");
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

        private void UpdateCategoryNewIndicators()
        {
            ApplyCategoryNewState(_newColorIndicator, HasNewForCategory(CosmeticCategory.Color));
            ApplyCategoryNewState(_newOjosIndicator, HasNewForCategory(CosmeticCategory.Ojos));
            ApplyCategoryNewState(_newOutfitIndicator, HasNewForCategory(CosmeticCategory.Outfit));
        }

        private bool HasNewForCategory(CosmeticCategory category)
        {
            if (_snapshot?.hasNewByCategory != null &&
                _snapshot.hasNewByCategory.TryGetValue(category, out bool summaryValue))
            {
                return summaryValue;
            }

            return _snapshot?.items?.Any(i => i.category == category && i.isNew) == true;
        }

        private static void ApplyCategoryNewState(VisualElement indicator, bool hasNew)
        {
            if (indicator == null)
            {
                return;
            }

            indicator.pickingMode = PickingMode.Ignore;
            indicator.style.display = hasNew ? DisplayStyle.Flex : DisplayStyle.None;
            indicator.style.visibility = hasNew ? Visibility.Visible : Visibility.Hidden;
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

            switch (category)
            {
                case CosmeticCategory.Color:
                    lockOverlay.AddToClassList("bloqueado");
                    return "bloqueado";
                case CosmeticCategory.Ojos:
                    lockOverlay.AddToClassList("bloqueadoOjos");
                    return "bloqueadoOjos";
                case CosmeticCategory.Outfit:
                    lockOverlay.AddToClassList("bloqueadoOutfit");
                    return "bloqueadoOutfit";
                default:
                    return "none";
            }
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
                UpdateCategoryNewIndicators();
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

            RegisterCategoryVisuals(
                CosmeticCategory.Color,
                "inv-color-blanco",
                new Dictionary<int, string>
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
                }
            );

            RegisterCategoryVisuals(
                CosmeticCategory.Ojos,
                "inv-ojos-default",
                new Dictionary<int, string>
                {
                    { defaults.defaultOjosItemId, "inv-ojos-default" },
                    { 12, "inv-ojos-rombo" },
                    { 13, "inv-ojos-cansado" },
                    { 14, "inv-ojos-estrella" },
                    { 15, "inv-ojos-happy" },
                    { 16, "inv-ojos-pirata" },
                    { 17, "inv-ojos-emputado" }
                }
            );

            RegisterCategoryVisuals(
                CosmeticCategory.Outfit,
                "inv-outfit-default",
                new Dictionary<int, string>
                {
                    { defaults.defaultOutfitItemId, "inv-outfit-default" },
                    { 19, "inv-outfit-engrane" },
                    { 20, "inv-outfit-rana" },
                    { 21, "inv-outfit-diablito" }
                }
            );

            AddBinding(root, 2, CosmeticCategory.Color, "cara1", "skinNaranja", "bloqueado1", "naranjaNuevo1");
            AddBinding(root, 3, CosmeticCategory.Color, "cara2", "skinMorado", "bloqueado2", "moradoNuevo2");
            AddBinding(root, 4, CosmeticCategory.Color, "cara3", "skinAmarillo", "bloqueado3", "amarilloNuevo3");
            AddBinding(root, 5, CosmeticCategory.Color, "cara4", "skinRojo", "bloqueado4", "rojoNuevo4");
            AddBinding(root, 6, CosmeticCategory.Color, "cara5", "skinTurquesa", "bloqueado5", "turquesaNuevo5");
            AddBinding(root, 7, CosmeticCategory.Color, "cara6", "skinVerde", "bloqueado6", "verdeNuevo6");
            AddBinding(root, 8, CosmeticCategory.Color, "cara7", "skinRosa", "bloqueado7", "rosaNuevo7");
            AddBinding(root, 9, CosmeticCategory.Color, "cara8", "skinAzul", "bloqueado8", "azulNuevo8");
            AddBinding(root, 10, CosmeticCategory.Color, "cara9", "skinNegro", "bloqueado9", "negroNuevo9");

            AddBinding(root, 12, CosmeticCategory.Ojos, "ojos1", "ojosRombo", "bloqueadoojos1", "romboNuevo1");
            AddBinding(root, 13, CosmeticCategory.Ojos, "ojos2", "ojosCansado", "bloqueadoojos2", "cansadosNuevo2");
            AddBinding(root, 14, CosmeticCategory.Ojos, "ojos3", "ojosEstrella", "bloqueadoojos3", "estrellaNuevo3");
            AddBinding(root, 15, CosmeticCategory.Ojos, "ojos4", "ojosHappy", "bloqueadoojos4", "happyNuevo4");
            AddBinding(root, 16, CosmeticCategory.Ojos, "ojos5", "ojosPirata", "bloqueadoojos5", "pirataNuevo5");
            AddBinding(root, 17, CosmeticCategory.Ojos, "ojos6", "ojosEmputado", "bloqueadoojos6", "emputadoNuevo6");

            AddBinding(root, 19, CosmeticCategory.Outfit, "outfit3", "outfitEngrane", "bloqueadoOutfit1", "engraneNuevo1");
            AddBinding(root, 20, CosmeticCategory.Outfit, "outfit1", "outfitRana", "bloqueadoOutfit2", "ranaNuevo2");
            AddBinding(root, 21, CosmeticCategory.Outfit, "outfit2", "outfitDiablito", "bloqueadoOutfit3", "diablitoNuevo3");
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
    }
}