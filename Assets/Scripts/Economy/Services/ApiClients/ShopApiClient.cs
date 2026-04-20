using System;
using System.Threading;
using System.Threading.Tasks;
using Somnia.Economy.DTOs;
using Somnia.Economy.Interfaces;
using UnityEngine;
using UnityEngine.Networking;

namespace Somnia.Economy.Services.ApiClients
{
    public class ShopApiClient : IShopApiClient
    {
        private readonly string _baseUrl;
        private readonly IPlayerSessionProvider _session;

        public ShopApiClient(string baseUrl, IPlayerSessionProvider session)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _session = session;
        }

        public async Task<ApiResponse<ShopCatalogResponse>> GetCatalogByIslandAsync(int playerId, int islandId, CancellationToken ct = default)
        {
            var url = $"{_baseUrl}/shops/islands/{islandId}/catalog?playerId={playerId}";
            try
            {
                using var req = UnityWebRequestExtensions.CreateGet(url, _session.Token);
                var body = await req.SendAsync(ct);
                return JsonUtility.FromJson<ApiResponse<ShopCatalogResponse>>(body);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"ShopApiClient GetCatalog error: {ex.Message}");
                return ApiResponse<ShopCatalogResponse>.Fail("network_error", ex.Message);
            }
        }

        public async Task<ApiResponse<PurchaseResponse>> RegisterPurchaseAsync(PurchaseRequest request, CancellationToken ct = default)
        {
            var url = $"{_baseUrl}/shops/purchases";
            try
            {
                var json = JsonUtility.ToJson(request);
                using var req = UnityWebRequestExtensions.CreateJsonRequest(url, UnityWebRequest.kHttpVerbPOST, json, _session.Token);
                var body = await req.SendAsync(ct);
                return JsonUtility.FromJson<ApiResponse<PurchaseResponse>>(body);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"ShopApiClient RegisterPurchase error: {ex.Message}");
                return ApiResponse<PurchaseResponse>.Fail("network_error", ex.Message);
            }
        }
    }
}
