using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Somnia.Economy.DTOs;
using Somnia.Economy.Interfaces;
using Somnia.UnityClient;
using UnityEngine;
using UnityEngine.Networking;

namespace Somnia.Economy.Services.ApiClients
{
    public class GameDataApiClient : IGameDataApiClient
    {
        private readonly string _baseUrl;
        private readonly IPlayerSessionProvider _session;

        public GameDataApiClient(string baseUrl, IPlayerSessionProvider session)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _session = session;
        }

        public async Task<ApiResponse<SlotSummary[]>> GetSlotsAsync(CancellationToken ct = default)
        {
            try
            {
                using var req = UnityWebRequestExtensions.CreateGet($"{_baseUrl}/game/slots", _session.Token);
                var body = await req.SendAsync(ct);
                var payload = JsonUtility.FromJson<SlotSummaryResponse>(body);
                if (payload == null || !payload.success)
                {
                    return ApiResponse<SlotSummary[]>.Fail("invalid_payload", "No fue posible cargar slots");
                }

                return ApiResponse<SlotSummary[]>.Ok(payload.slots ?? Array.Empty<SlotSummary>());
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"GameDataApiClient GetSlots error: {ex.Message}");
                return ApiResponse<SlotSummary[]>.Fail("network_error", ex.Message);
            }
        }

        public Task<ApiResponse<SlotDetailResponse>> GetSlotDetailAsync(int slotNumber, CancellationToken ct = default) =>
            GetSlotDetailFromEndpointAsync($"{_baseUrl}/game/slots/{slotNumber}", ct);

        public async Task<ApiResponse<SlotDetailResponse>> CreateSlotAsync(CreateSlotRequest request, CancellationToken ct = default)
        {
            var response = await SendEnvelopeAsync<SlotDetailResponse>($"{_baseUrl}/game/slots", UnityWebRequest.kHttpVerbPOST, request, ct);
            if (response.success) return response;
            return response.error?.code == "invalid_payload"
                ? await GetSlotDetailAsync(request.slot_numero, ct)
                : response;
        }

        public async Task<ApiResponse<SlotDetailResponse>> InitializeNewGameAsync(int slotNumber, InitializeGameRequest request, CancellationToken ct = default)
        {
            var response = await SendEnvelopeAsync<SlotDetailResponse>($"{_baseUrl}/game/slots/{slotNumber}/initialize", UnityWebRequest.kHttpVerbPOST, request, ct);
            if (response.success) return response;
            return response.error?.code == "invalid_payload"
                ? await GetSlotDetailAsync(slotNumber, ct)
                : response;
        }

        public Task<ApiResponse<List<ProgressData>>> SaveProgressAsync(int slotNumber, SaveProgressRequest request, CancellationToken ct = default) =>
            SendEnvelopeAsync<List<ProgressData>>($"{_baseUrl}/game/slots/{slotNumber}/progress", UnityWebRequest.kHttpVerbPUT, request, ct);

        public Task<ApiResponse<EquippedItemsData>> UpdateEquipmentAsync(int slotNumber, UpdateEquipmentRequest request, CancellationToken ct = default) =>
            SendEnvelopeAsync<EquippedItemsData>($"{_baseUrl}/game/slots/{slotNumber}/equipment", UnityWebRequest.kHttpVerbPUT, request, ct);

        private async Task<ApiResponse<SlotDetailResponse>> GetSlotDetailFromEndpointAsync(string url, CancellationToken ct)
        {
            try
            {
                using var req = UnityWebRequestExtensions.CreateGet(url, _session.Token);
                var body = await req.SendAsync(ct);
                var payload = JsonUtility.FromJson<SlotDetailResponse>(body);
                if (payload == null || !payload.success || payload.slot == null)
                {
                    return ApiResponse<SlotDetailResponse>.Fail("invalid_payload", "No fue posible cargar detalle de slot");
                }

                return ApiResponse<SlotDetailResponse>.Ok(payload);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"GameDataApiClient SlotDetail error: {ex.Message}");
                return ApiResponse<SlotDetailResponse>.Fail("network_error", ex.Message);
            }
        }

        private async Task<ApiResponse<T>> SendEnvelopeAsync<T>(string url, string method, object payload, CancellationToken ct)
        {
            try
            {
                var json = JsonUtility.ToJson(payload);
                using var req = UnityWebRequestExtensions.CreateJsonRequest(url, method, json, _session.Token);
                var body = await req.SendAsync(ct);

                var envelope = JsonUtility.FromJson<ApiResponse<T>>(body);
                if (envelope != null && (envelope.success || envelope.data != null))
                {
                    return envelope;
                }

                var directPayload = JsonUtility.FromJson<T>(body);
                if (directPayload != null)
                {
                    return ApiResponse<T>.Ok(directPayload);
                }

                return ApiResponse<T>.Fail("invalid_payload", "Respuesta no reconocida del backend");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"GameDataApiClient {method} error: {ex.Message}");
                return ApiResponse<T>.Fail("network_error", ex.Message);
            }
        }
    }
}
