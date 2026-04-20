using System;
using System.Threading;
using System.Threading.Tasks;
using Somnia.Economy.DTOs;
using Somnia.Economy.Interfaces;
using UnityEngine;
using UnityEngine.Networking;

namespace Somnia.Economy.Services.ApiClients
{
    public class PlayerEconomyApiClient : IPlayerEconomyApiClient
    {
        private readonly string _baseUrl;
        private readonly IPlayerSessionProvider _session;

        public PlayerEconomyApiClient(string baseUrl, IPlayerSessionProvider session)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _session = session;
        }

        public Task<ApiResponse<PlayerEconomyData>> GetPlayerEconomyAsync(int playerId, CancellationToken ct = default) =>
            GetAsync<PlayerEconomyData>($"{_baseUrl}/economy/players/{playerId}", ct);

        public Task<ApiResponse<EconomyBalanceResponse>> GetBalanceAsync(int playerId, CancellationToken ct = default) =>
            GetAsync<EconomyBalanceResponse>($"{_baseUrl}/economy/players/{playerId}/balance", ct);

        public Task<ApiResponse<RangeAgeData>> GetRangeAgeAsync(int playerId, CancellationToken ct = default) =>
            GetAsync<RangeAgeData>($"{_baseUrl}/economy/players/{playerId}/range-age", ct);

        public Task<ApiResponse<EconomyBalanceResponse>> UpdateBalanceAsync(UpdateBalanceRequest request, CancellationToken ct = default) =>
            SendAsync<EconomyBalanceResponse>($"{_baseUrl}/economy/players/{request.id_jugador}/balance", UnityWebRequest.kHttpVerbPUT, request, ct);

        private async Task<ApiResponse<T>> GetAsync<T>(string url, CancellationToken ct)
        {
            try
            {
                using var req = UnityWebRequestExtensions.CreateGet(url, _session.Token);
                var body = await req.SendAsync(ct);
                return JsonUtility.FromJson<ApiResponse<T>>(body);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"PlayerEconomyApiClient GET error: {ex.Message}");
                return ApiResponse<T>.Fail("network_error", ex.Message);
            }
        }

        private async Task<ApiResponse<T>> SendAsync<T>(string url, string method, object payload, CancellationToken ct)
        {
            try
            {
                var json = JsonUtility.ToJson(payload);
                using var req = UnityWebRequestExtensions.CreateJsonRequest(url, method, json, _session.Token);
                var body = await req.SendAsync(ct);
                return JsonUtility.FromJson<ApiResponse<T>>(body);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"PlayerEconomyApiClient {method} error: {ex.Message}");
                return ApiResponse<T>.Fail("network_error", ex.Message);
            }
        }
    }
}
