using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Somnia.Tienda;
using Somnia.UnityClient;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class SistemaTienda : MonoBehaviour
{
    [Header("Backend")]
    [SerializeField] private string apiBaseUrl = ApiConfig.DefaultApiBaseUrl;

    private CancellationTokenSource _cts;

    public ShopResponseDto CurrentShop { get; private set; }
    public int CurrentYatzis => CurrentShop?.yatzis ?? 0;

    private string BuildUrl(string endpoint) => $"{apiBaseUrl.TrimEnd('/')}{endpoint}";

    private void OnDisable()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    public async Task<ShopResponseDto> LoadShopAsync(int slotNumber)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        var endpoint = $"/game/slots/{slotNumber}/shop";
        var url = BuildUrl(endpoint);

        Debug.Log($"[Tienda] Cargando tienda real. slot={slotNumber} url={url}");

        try
        {
            using var request = CreateGet(url);
            string body = await SendAsync(request, _cts.Token);
            var response = JsonUtility.FromJson<ShopResponseDto>(body);

            if (response == null || !response.success)
            {
                throw new InvalidOperationException($"Respuesta inválida al cargar tienda. body={body}");
            }

            response.items ??= new List<ShopItemDto>();
            CurrentShop = response;

            Debug.Log($"[Tienda] Slot detectado={slotNumber} id_partida={response.id_partida} yatzis_iniciales={response.yatzis} items_cargados={response.items.Count}");
            return CurrentShop;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Tienda] Error cargando tienda real: {ex.Message}");
            throw;
        }
    }

    public async Task<BuyShopItemResponseDto> BuyShopItemAsync(int slotNumber, int idTiendaItem)
    {
        if (CurrentShop == null)
        {
            throw new InvalidOperationException("LoadShopAsync debe ejecutarse antes de comprar.");
        }

        var item = FindItem(idTiendaItem);
        if (item == null)
        {
            throw new InvalidOperationException($"No existe item id_tienda_item={idTiendaItem} en el estado local.");
        }

        if (item.IsSoldOut)
        {
            throw new InvalidOperationException($"El item id_tienda_item={idTiendaItem} ya fue comprado (sold_out). ");
        }

        if (CurrentShop.yatzis < item.costo)
        {
            throw new InvalidOperationException($"Saldo insuficiente. yatzis={CurrentShop.yatzis} costo={item.costo}");
        }

        var endpoint = $"/game/slots/{slotNumber}/shop/buy";
        var url = BuildUrl(endpoint);

        var payload = new BuyShopItemRequestDto { id_tienda_item = idTiendaItem };
        string json = JsonUtility.ToJson(payload);

        Debug.Log($"[Tienda] Intentando compra. slot={slotNumber} id_tienda_item={idTiendaItem} costo={item.costo}");

        try
        {
            using var request = CreatePost(url, json);
            string body = await SendAsync(request, _cts?.Token ?? CancellationToken.None);
            var response = JsonUtility.FromJson<BuyShopItemResponseDto>(body);

            if (response == null || !response.success || response.data == null)
            {
                throw new InvalidOperationException($"Respuesta inválida en compra. body={body}");
            }

            ApplyPurchaseResult(response.data);
            Debug.Log($"[Tienda] Compra exitosa id_tienda_item={idTiendaItem} id_item={response.data.id_item} saldo_final={CurrentShop.yatzis}");
            return response;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Tienda] Error comprando id_tienda_item={idTiendaItem}: {ex.Message}");
            throw;
        }
    }

    public IReadOnlyList<ShopItemDto> GetItemsByCategory(ShopCategory category)
    {
        if (CurrentShop?.items == null)
        {
            return Array.Empty<ShopItemDto>();
        }

        List<ShopItemDto> filtered = new();
        foreach (var item in CurrentShop.items)
        {
            if (item.ResolveCategory() == category)
            {
                filtered.Add(item);
            }
        }

        return filtered;
    }

    private void ApplyPurchaseResult(BuyShopItemResultDto result)
    {
        CurrentShop.yatzis = result.yatzis;

        ShopItemDto item = FindItem(result.id_tienda_item);
        if (item == null)
        {
            return;
        }

        item.owned = result.owned || result.nuevo == 1;
        item.sold_out = result.sold_out || item.owned;
    }

    private ShopItemDto FindItem(int idTiendaItem)
    {
        if (CurrentShop?.items == null)
        {
            return null;
        }

        for (int i = 0; i < CurrentShop.items.Count; i++)
        {
            if (CurrentShop.items[i].id_tienda_item == idTiendaItem)
            {
                return CurrentShop.items[i];
            }
        }

        return null;
    }

    private UnityWebRequest CreateGet(string url)
    {
        var request = UnityWebRequest.Get(url);
        request.downloadHandler = new DownloadHandlerBuffer();
        AttachAuth(request);
        return request;
    }

    private UnityWebRequest CreatePost(string url, string json)
    {
        var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
        {
            uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json ?? "{}")),
            downloadHandler = new DownloadHandlerBuffer()
        };

        request.SetRequestHeader("Content-Type", "application/json");
        AttachAuth(request);
        return request;
    }

    private void AttachAuth(UnityWebRequest request)
    {
        string token = GameSessionManager.Instance?.AccessToken;
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.SetRequestHeader("Authorization", $"Bearer {token}");
        }
    }

    private static async Task<string> SendAsync(UnityWebRequest request, CancellationToken ct)
    {
        using var registration = ct.Register(request.Abort);
        var op = request.SendWebRequest();

        while (!op.isDone)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Yield();
        }

        string body = request.downloadHandler?.text ?? string.Empty;

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            throw new InvalidOperationException($"HTTP {(int)request.responseCode}: {request.error} - {body}");
        }

        return body;
    }
}