using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    private Label etiquetaMonedas;
    private Label monedasFallback;

    private SistemaTienda sistemaCompra;
    private readonly Dictionary<int, Sprite> _spriteByItemId = new();
    private ShopCategory _categoriaActiva = ShopCategory.Color;
    private bool _initialized;

    private EventCallback<ClickEvent> _onOjos;
    private EventCallback<ClickEvent> _onColor;
    private EventCallback<ClickEvent> _onRopa;

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
        etiquetaMonedas = root.Q<Label>("EtiquetaMonedas");
        monedasFallback = root.Q<Label>("Monedas");

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
        Debug.Log($"[TiendaUI] Slot detectado desde GameSessionManager: {slot}");

        try
        {
            var response = await sistemaCompra.LoadShopAsync(slot);
            Debug.Log($"[TiendaUI] yatzis iniciales={response.yatzis} items cargados={response.items.Count}");

            ActualizarMonedas(response.yatzis);
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
        var etiquetaPrecio = instancia.Q<Label>("Precio");
        var btnCompra = instancia.Q<Button>("BtnCompra");
        var soldOut = instancia.Q<VisualElement>("SoldOut");

        if (fondo != null && _spriteByItemId.TryGetValue(item.id_item, out Sprite sprite) && sprite != null)
        {
            fondo.style.backgroundImage = new StyleBackground(sprite);
        }

        if (etiquetaPrecio != null)
        {
            etiquetaPrecio.text = item.costo.ToString();
        }

        bool isSoldOut = item.IsSoldOut;
        if (soldOut != null)
        {
            soldOut.style.display = isSoldOut ? DisplayStyle.Flex : DisplayStyle.None;
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

            ActualizarMonedas(response.data.yatzis);
            Debug.Log($"[TiendaUI] item comprado={item.id_item} id_tienda_item={item.id_tienda_item} saldo final={response.data.yatzis}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TiendaUI] Error al comprar id_tienda_item={item.id_tienda_item}: {ex.Message}");
        }
    }

    private void ActualizarMonedas(int yatzis)
    {
        string texto = yatzis.ToString();

        if (etiquetaMonedas != null)
        {
            etiquetaMonedas.text = texto;
        }

        if (monedasFallback != null)
        {
            monedasFallback.text = texto;
        }
    }

    private void ConstruirMapaSprites()
    {
        _spriteByItemId.Clear();

        if (spritesPorItem == null)
        {
            return;
        }

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