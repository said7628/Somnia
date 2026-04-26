using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Somnia.Tienda;
using Somnia.UnityClient;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
[RequireComponent(typeof(SistemaTienda))]
public class ControladorTienda : MonoBehaviour
{
    [Serializable]
    private struct ItemSpriteBinding
    {
        public int idItem;
        public Sprite sprite;
    }

    [Header("El Molde del Estandarte")]
    [SerializeField] private VisualTreeAsset plantillaItemTienda;

    [Header("Mapeo visual por id_item")]
    [SerializeField] private ItemSpriteBinding[] spritesPorItem;

    [Header("Imágenes Estado NORMAL")]
    [SerializeField] private Sprite imgOjosNormal;
    [SerializeField] private Sprite imgColorNormal;
    [SerializeField] private Sprite imgRopaNormal;

    [Header("Imágenes Estado ACTIVO")]
    [SerializeField] private Sprite imgOjosActivo;
    [SerializeField] private Sprite imgColorActivo;
    [SerializeField] private Sprite imgRopaActivo;

    private UIDocument tiendaUI;
    private Button btnOjos;
    private Button btnColor;
    private Button btnRopa;
    private VisualElement contenedorOpciones;
    private Label _etiquetaMonedas;

    private SistemaTienda sistemaCompra;
    private readonly Dictionary<int, Sprite> _spriteByItemId = new();
    private ShopCategory _categoriaActiva = ShopCategory.Color;
    private bool _initialized;
    private bool _loggedMissingBindingsHint;

    private EventCallback<ClickEvent> _onOjos;
    private EventCallback<ClickEvent> _onColor;
    private EventCallback<ClickEvent> _onRopa;

    private static readonly Dictionary<int, string[]> SpriteKeywordsByItemId = new()
    {
        { 2, new[] { "naranja" } },
        { 3, new[] { "morado" } },
        { 4, new[] { "amarillo", "amarillon" } },
        { 5, new[] { "rojo" } },
        { 6, new[] { "turquesa" } },
        { 7, new[] { "verde" } },
        { 8, new[] { "rosa" } },
        { 9, new[] { "azul" } },
        { 10, new[] { "negro" } },
        { 12, new[] { "rombo", "rombos" } },
        { 13, new[] { "cansado" } },
        { 14, new[] { "estrella", "estrellas" } },
        { 15, new[] { "happy", "marihuano" } },
        { 16, new[] { "pirata" } },
        { 17, new[] { "emputado", "enojado" } },
        { 19, new[] { "engrane" } },
        { 20, new[] { "rana" } },
        { 21, new[] { "diablito" } }
    };

    private void OnEnable()
    {
        tiendaUI = GetComponent<UIDocument>();
        sistemaCompra = GetComponent<SistemaTienda>();

        ConstruirMapaSprites();
        BindUi();

        _ = InicializarTiendaAsync();
    }

    private void OnDisable()
    {
        if (btnOjos != null && _onOjos != null)
        {
            btnOjos.UnregisterCallback(_onOjos);
        }

        if (btnColor != null && _onColor != null)
        {
            btnColor.UnregisterCallback(_onColor);
        }

        if (btnRopa != null && _onRopa != null)
        {
            btnRopa.UnregisterCallback(_onRopa);
        }
    }

    private void BindUi()
    {
        var root = tiendaUI.rootVisualElement;

        btnOjos = root.Q<Button>("BtnOjos");
        btnColor = root.Q<Button>("BtnColor");
        btnRopa = root.Q<Button>("BtnRopa");
        contenedorOpciones = root.Q<VisualElement>("ContenedorOpciones");
        _etiquetaMonedas = root.Q<Label>("EtiquetaMonedas");
        Debug.Log($"[TiendaUI] EtiquetaMonedas found={(_etiquetaMonedas != null)}");
        _onOjos = _ => CambiarCategoria(ShopCategory.Ojos);
        _onColor = _ => CambiarCategoria(ShopCategory.Color);
        _onRopa = _ => CambiarCategoria(ShopCategory.Outfit);

        btnOjos?.RegisterCallback(_onOjos);
        btnColor?.RegisterCallback(_onColor);
        btnRopa?.RegisterCallback(_onRopa);

        ResetearBotones();
    }

    private async Task InicializarTiendaAsync()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;

        if (plantillaItemTienda == null)
        {
            Debug.LogError("[TiendaUI] Falta asignar PlantillaItemTienda en el inspector.");
            return;
        }

        int slot = Mathf.Max(1, GameSessionManager.Instance?.CurrentSlotNumber ?? 1);
        int currentIslandId = ResolveCurrentIslandId();
        Debug.Log($"[TiendaUI] Slot detectado desde GameSessionManager: {slot}");

        try
        {
            var response = await sistemaCompra.LoadShopAsync(slot, currentIslandId);
            Debug.Log($"[TiendaUI] yatzis backend={response.yatzis}");
            Debug.Log($"[TiendaUI] yatzis iniciales={response.yatzis} items cargados={response.items.Count}");

            UpdateCurrencyLabel(response.yatzis);
            CambiarCategoria(_categoriaActiva);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TiendaUI] No se pudo inicializar la tienda real: {ex.Message}");
            contenedorOpciones?.Clear();
        }
    }

    private void CambiarCategoria(ShopCategory categoria)
    {
        _categoriaActiva = categoria;

        Debug.Log($"[TiendaUI] Categoría seleccionada: {_categoriaActiva}");

        ResetearBotones();
        AplicarBotonActivo(categoria);

        RenderCategoria(categoria);
    }

    private void RenderCategoria(ShopCategory categoria)
    {
        if (contenedorOpciones == null)
        {
            return;
        }

        contenedorOpciones.Clear();

        IReadOnlyList<ShopItemDto> items = sistemaCompra.GetItemsByCategory(categoria);
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var instancia = plantillaItemTienda.Instantiate();
            ConfigurarTarjeta(instancia, item);
            contenedorOpciones.Add(instancia);
        }
    }

    private void ConfigurarTarjeta(TemplateContainer instancia, ShopItemDto item)
    {
        var fondo = instancia.Q<VisualElement>("FondoEstandarte");
        var etiquetaPrecio = instancia.Q<Label>("Precio") ?? fondo?.Q<Label>("Precio");
        var btnCompra = instancia.Q<Button>("BtnCompra");
        var soldOut = instancia.Q<VisualElement>("SoldOut");

        // El fondo mantiene el estandarte de la plantilla.
        // El cosmético visible se pinta sobre BtnCompra (o fondo como fallback).
        var contenedorSpriteItem = (VisualElement)btnCompra ?? fondo;
        Sprite sprite = ResolveSpriteForItem(item);
        if (contenedorSpriteItem != null && sprite != null)
        {
            contenedorSpriteItem.style.backgroundImage = new StyleBackground(sprite);
            contenedorSpriteItem.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            string targetName = string.IsNullOrWhiteSpace(contenedorSpriteItem.name) ? "VisualElement" : contenedorSpriteItem.name;
            Debug.Log($"[ShopCard] id_item={item.id_item} nombre={item.nombre} sprite={sprite.name} appliedTo={targetName}");
        }
        else if (contenedorSpriteItem != null)
        {
            contenedorSpriteItem.style.backgroundImage = StyleKeyword.None;
            string targetName = string.IsNullOrWhiteSpace(contenedorSpriteItem.name) ? "VisualElement" : contenedorSpriteItem.name;
            Debug.LogWarning($"[ShopCard] id_item={item.id_item} nombre={item.nombre} sprite=null appliedTo={targetName}. Ejecuta Auto-asignar sprites de tienda en el inspector.");
        }

        if (etiquetaPrecio != null)
        {
            etiquetaPrecio.text = item.costo.ToString();
            etiquetaPrecio.style.display = DisplayStyle.Flex;
            etiquetaPrecio.style.visibility = Visibility.Visible;
            etiquetaPrecio.style.position = Position.Absolute;
            etiquetaPrecio.style.bottom = 12;
            etiquetaPrecio.style.left = 50;
            etiquetaPrecio.style.unityTextAlign = TextAnchor.MiddleLeft;
            etiquetaPrecio.style.color = Color.white;
            etiquetaPrecio.style.unityFontStyleAndWeight = FontStyle.Bold;
            etiquetaPrecio.style.fontSize = 28;
            etiquetaPrecio.BringToFront();

            Debug.Log($"[ShopPrice] id_item={item.id_item} precio={item.costo} labelFound=True broughtToFront=True");
        }
        else
        {
            Debug.LogWarning($"[ShopPrice] id_item={item.id_item} precio={item.costo} labelFound=False broughtToFront=False");
        }

        bool isSoldOut = item.IsSoldOut;
        if (soldOut != null)
        {
            soldOut.style.display = isSoldOut ? DisplayStyle.Flex : DisplayStyle.None;
            soldOut.BringToFront();
        }

        if (btnCompra != null)
        {
            btnCompra.clicked += () => _ = IntentarCompraAsync(item, soldOut);
        }

        instancia.style.marginLeft = 15;
        instancia.style.marginRight = 15;
    }

    private async Task IntentarCompraAsync(ShopItemDto item, VisualElement soldOut)
    {
        if (item == null)
        {
            return;
        }

        if (item.IsSoldOut)
        {
            Debug.LogWarning($"[TiendaUI] Compra bloqueada. item={item.id_item} ya está owned/sold_out.");
            return;
        }

        if (sistemaCompra.CurrentYatzis < item.costo)
        {
            Debug.LogWarning($"[TiendaUI] Saldo insuficiente para comprar item={item.id_item}. saldo={sistemaCompra.CurrentYatzis} costo={item.costo}");
            return;
        }

        int slot = Mathf.Max(1, GameSessionManager.Instance?.CurrentSlotNumber ?? 1);

        try
        {
            var response = await sistemaCompra.BuyShopItemAsync(slot, item.id_tienda_item);

            if (response?.data == null)
            {
                Debug.LogWarning("[TiendaUI] La compra no devolvió payload data.");
                return;
            }

            item.owned = response.data.owned || response.data.nuevo == 1;
            item.sold_out = response.data.sold_out || item.owned;

            if (soldOut != null)
            {
                soldOut.style.display = DisplayStyle.Flex;
            }

            UpdateCurrencyLabel(response.data.yatzis);
            Debug.Log($"[TiendaUI] item comprado={item.id_item} id_tienda_item={item.id_tienda_item} saldo final={response.data.yatzis}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TiendaUI] Error al comprar id_tienda_item={item.id_tienda_item}: {ex.Message}");
        }
    }

    private void UpdateCurrencyLabel(int yatzis)
    {
        if (_etiquetaMonedas != null)
        {
            _etiquetaMonedas.text = yatzis.ToString();
        }

        Debug.Log($"[TiendaUI] EtiquetaMonedas actualizado={yatzis}");
    }

    private static int ResolveCurrentIslandId()
    {
        int fromSession = Mathf.Max(0, GameSessionManager.Instance?.CurrentIslandId ?? 0);
        if (fromSession > 0)
        {
            Debug.Log($"[Shop] currentIslandId={fromSession}");
            return fromSession;
        }

        string fallbackScene = Memoria_Islas.islaDeDondeVengo;
        if (!string.IsNullOrWhiteSpace(fallbackScene))
        {
            string normalized = fallbackScene.Trim().ToLowerInvariant();
            if (normalized.Contains("isla 3") || normalized.Contains("isla3") || normalized.Contains("ciudad"))
            {
                return 3;
            }

            if (normalized.Contains("isla 2") || normalized.Contains("isla2") || normalized.Contains("nieve"))
            {
                return 2;
            }
        }

        return 1;
    }

    private void ConstruirMapaSprites()
    {
        _spriteByItemId.Clear();

        if (spritesPorItem != null)
        {
            for (int i = 0; i < spritesPorItem.Length; i++)
            {
                var binding = spritesPorItem[i];
                if (binding.idItem <= 0 || binding.sprite == null)
                {
                    continue;
                }

                _spriteByItemId[binding.idItem] = binding.sprite;
            }
        }

    }

    private Sprite ResolveSpriteForItem(ShopItemDto item)
    {
        if (item == null || item.id_item <= 0)
        {
            return null;
        }

        if (_spriteByItemId.TryGetValue(item.id_item, out Sprite sprite) && sprite != null)
        {
            return sprite;
        }

        if ((_spriteByItemId.Count == 0 || spritesPorItem == null || spritesPorItem.Length == 0) && !_loggedMissingBindingsHint)
        {
            Debug.LogWarning("[ShopVisual] spritesPorItem está vacío. Ejecuta Auto-asignar sprites de tienda en el inspector.");
            _loggedMissingBindingsHint = true;
        }

        return null;
    }

    private static List<string> BuildSearchKeywords(int itemId, string itemName)
    {
        HashSet<string> terms = new(StringComparer.OrdinalIgnoreCase);

        if (SpriteKeywordsByItemId.TryGetValue(itemId, out string[] mappedKeywords))
        {
            for (int i = 0; i < mappedKeywords.Length; i++)
            {
                AddTerm(mappedKeywords[i], terms);
            }
        }

        AddTerm(itemName, terms);

        return new List<string>(terms);
    }

    private static void AddTerm(string raw, HashSet<string> terms)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        string normalized = raw.Trim().ToLowerInvariant();
        if (normalized.Length < 3)
        {
            return;
        }

        terms.Add(normalized);
    }

#if UNITY_EDITOR
    [ContextMenu("Auto-asignar sprites de tienda")]
    private void AutoAsignarSpritesDeTienda()
    {
        AutoAsignarSpritesDeTiendaInternal();
    }

    private bool AutoAsignarSpritesDeTiendaInternal()
    {
        const string searchRoot = "Assets/Imagenes Somnia/Tienda";
        List<ItemSpriteBinding> bindings = new();
        int found = 0;
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Texture2D", new[] { searchRoot });
        string[] paths = guids
            .Select(UnityEditor.AssetDatabase.GUIDToAssetPath)
            .Where(path => string.Equals(Path.GetExtension(path), ".png", StringComparison.OrdinalIgnoreCase))
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var kvp in SpriteKeywordsByItemId)
        {
            string keyword = ResolvePrimaryKeyword(kvp.Key);
            List<string> keywords = BuildSearchKeywords(kvp.Key, keyword);
            if (!TryResolveFromAssetDatabase(paths, keyword, keywords, out Sprite sprite, out string matchedPath))
            {
                Debug.LogWarning($"[ShopAutoAssign] id_item={kvp.Key} no_match keywords={string.Join("|", keywords)}");
                continue;
            }

            bindings.Add(new ItemSpriteBinding
            {
                idItem = kvp.Key,
                sprite = sprite
            });
            found++;
            Debug.Log($"[ShopAutoAssign] id_item={kvp.Key} keyword={keyword} path={matchedPath} sprite={sprite.name}");
        }

        spritesPorItem = bindings.ToArray();
        UnityEditor.EditorUtility.SetDirty(this);
        if (gameObject.scene.IsValid())
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
        Debug.Log($"[ShopAutoAssign] Auto-asignación completada. encontrados={found} total={SpriteKeywordsByItemId.Count}");

        ConstruirMapaSprites();
        return found > 0;
    }

    private static string ResolvePrimaryKeyword(int itemId)
    {
        return SpriteKeywordsByItemId.TryGetValue(itemId, out string[] keywords) && keywords.Length > 0
            ? NormalizeForMatch(keywords[0])
            : string.Empty;
    }

    private static bool TryResolveFromAssetDatabase(string[] paths, string primaryKeyword, List<string> keywords, out Sprite sprite, out string matchedPath)
    {
        string normalizedPrimaryKeyword = NormalizeForMatch(primaryKeyword);
        List<string> orderedCandidates = paths
            .Where(path => FileNameContains(path, normalizedPrimaryKeyword))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (orderedCandidates.Count == 0)
        {
            orderedCandidates = paths
                .Where(path => ContainsAny(NormalizeForMatch(Path.GetFileNameWithoutExtension(path)), keywords))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        for (int i = 0; i < orderedCandidates.Count; i++)
        {
            string path = orderedCandidates[i];
            if (TryChooseSpriteFromPath(path, normalizedPrimaryKeyword, out sprite))
            {
                matchedPath = path;
                return true;
            }
        }

        sprite = null;
        matchedPath = string.Empty;
        return false;
    }

    private static bool TryChooseSpriteFromPath(string path, string primaryKeyword, out Sprite sprite)
    {
        string normalizedFileName = NormalizeForMatch(Path.GetFileNameWithoutExtension(path));
        List<Sprite> sprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Where(candidate => candidate != null).ToList();

        Sprite bestByKeyword = sprites.FirstOrDefault(candidate => NormalizeForMatch(candidate.name).Contains(primaryKeyword));
        if (bestByKeyword != null)
        {
            sprite = bestByKeyword;
            return true;
        }

        Sprite bestByFileName = sprites.FirstOrDefault(candidate => NormalizeForMatch(candidate.name).Contains(normalizedFileName));
        if (bestByFileName != null)
        {
            sprite = bestByFileName;
            return true;
        }

        if (sprites.Count == 1)
        {
            sprite = sprites[0];
            return true;
        }

        if (sprites.Count > 1)
        {
            sprite = sprites
                .OrderByDescending(candidate => candidate.rect.width * candidate.rect.height)
                .FirstOrDefault();
            if (sprite != null)
            {
                return true;
            }
        }

        Texture2D texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture != null)
        {
            Debug.LogWarning($"[ShopAutoAssign] path_match_sin_sprite path={path} texture={texture.name}. Revisa import settings/slices.");
        }

        sprite = null;
        return false;
    }

    private static bool FileNameContains(string path, string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        string normalizedFileName = NormalizeForMatch(Path.GetFileNameWithoutExtension(path));
        return normalizedFileName.Contains(token);
    }

    private static string NormalizeForMatch(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        char[] filtered = value
            .Trim()
            .ToLowerInvariant()
            .Where(ch => char.IsLetterOrDigit(ch))
            .ToArray();

        return new string(filtered);
    }

    private static bool ContainsAny(string source, List<string> keywords)
    {
        string normalizedSource = NormalizeForMatch(source);
        for (int i = 0; i < keywords.Count; i++)
        {
            string keyword = NormalizeForMatch(keywords[i]);
            if (string.IsNullOrEmpty(keyword))
            {
                continue;
            }
            if (normalizedSource.Contains(keyword))
            {
                return true;
            }
        }

        return false;
    }
#endif

    private void ResetearBotones()
    {
        if (imgOjosNormal != null && btnOjos != null)
        {
            btnOjos.style.backgroundImage = new StyleBackground(imgOjosNormal);
        }

        if (imgColorNormal != null && btnColor != null)
        {
            btnColor.style.backgroundImage = new StyleBackground(imgColorNormal);
        }

        if (imgRopaNormal != null && btnRopa != null)
        {
            btnRopa.style.backgroundImage = new StyleBackground(imgRopaNormal);
        }
    }

    private void AplicarBotonActivo(ShopCategory categoria)
    {
        switch (categoria)
        {
            case ShopCategory.Color:
                if (imgColorActivo != null && btnColor != null)
                {
                    btnColor.style.backgroundImage = new StyleBackground(imgColorActivo);
                }
                break;

            case ShopCategory.Ojos:
                if (imgOjosActivo != null && btnOjos != null)
                {
                    btnOjos.style.backgroundImage = new StyleBackground(imgOjosActivo);
                }
                break;

            case ShopCategory.Outfit:
                if (imgRopaActivo != null && btnRopa != null)
                {
                    btnRopa.style.backgroundImage = new StyleBackground(imgRopaActivo);
                }
                break;
        }
    }
}