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


        private const bool EnableHttpDebugLogs = true;

        public GameDataApiClient(string baseUrl, IPlayerSessionProvider session)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _session = session;

            if (EnableHttpDebugLogs)
            {
                Debug.Log($"[GameDataApiClient] Base URL resuelta: {_baseUrl}");
            }
        }

        public async Task<ApiResponse<SlotSummary[]>> GetSlotsAsync(CancellationToken ct = default)
        {
            var url = $"{_baseUrl}/game/slots";

            try
            {
                using var req = UnityWebRequestExtensions.CreateGet(url, _session.Token);
                var body = await req.SendAsync(ct);

                if (EnableHttpDebugLogs)
                {
                    Debug.Log($"[HTTP] GET {url} -> {(int)req.responseCode} | Body: {body}");
                }

                var payload = JsonUtility.FromJson<SlotSummaryResponse>(body);
                if (payload == null || !payload.success)
                {
                    return ApiResponse<SlotSummary[]>.Fail("invalid_payload", "No fue posible cargar slots");
                }

                return ApiResponse<SlotSummary[]>.Ok(payload.slots ?? Array.Empty<SlotSummary>());
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"GameDataApiClient GetSlots error [{url}]: {ex.Message}");
                return ApiResponse<SlotSummary[]>.Fail("network_error", ex.Message);
            }
        }

        public Task<ApiResponse<SlotDetailResponse>> GetSlotDetailAsync(int slotNumber, CancellationToken ct = default) =>
            GetSlotDetailFromEndpointAsync($"{_baseUrl}/game/slots/{slotNumber}", ct);

        public Task<ApiResponse<bool>> DeleteSlotAsync(int slotNumber, CancellationToken ct = default) =>
            SendNoContentEnvelopeAsync($"{_baseUrl}/game/slots/{slotNumber}", UnityWebRequest.kHttpVerbDELETE, ct);

        public async Task<ApiResponse<SlotDetailResponse>> CreateSlotAsync(CreateSlotRequest request, CancellationToken ct = default)
        {
            var url = $"{_baseUrl}/game/slots";
            var response = await SendEnvelopeAsync<SlotDetailResponse>(url, UnityWebRequest.kHttpVerbPOST, request, ct);
            if (response.success) return response;
            return response.error?.code == "invalid_payload"
                ? await GetSlotDetailAsync(request.slot_numero, ct)
                : response;
        }

        public async Task<ApiResponse<SlotDetailResponse>> InitializeNewGameAsync(int slotNumber, InitializeGameRequest request, CancellationToken ct = default)
        {
            var url = $"{_baseUrl}/game/slots/{slotNumber}/initialize";
            var response = await SendEnvelopeAsync<SlotDetailResponse>(url, UnityWebRequest.kHttpVerbPOST, request, ct);
            if (response.success) return response;
            return response.error?.code == "invalid_payload"
                ? await GetSlotDetailAsync(slotNumber, ct)
                : response;
        }

        public Task<ApiResponse<List<ProgressData>>> SaveProgressAsync(int slotNumber, SaveProgressRequest request, CancellationToken ct = default) =>
            SendEnvelopeAsync<List<ProgressData>>($"{_baseUrl}/game/slots/{slotNumber}/progress", UnityWebRequest.kHttpVerbPUT, request, ct);

        public async Task<ApiResponse<ProgressSaveResult>> SaveProgressEntryAsync(int slotNumber, ProgressData progress, CancellationToken ct = default)
        {
            if (progress == null)
            {
                return ApiResponse<ProgressSaveResult>.Fail("invalid_progress", "El progreso a guardar es nulo.");
            }

            var payload = new SaveProgressCompatRequest
            {
                slot_numero = slotNumber,
                slotNumber = slotNumber,
                id_nivel = progress.id_nivel,
                idNivel = progress.id_nivel,
                puntuacion_maxima = Mathf.Max(0, progress.puntuacion_maxima),
                puntuacionMaxima = Mathf.Max(0, progress.puntuacion_maxima),
                score = Mathf.Max(0, progress.puntuacion_maxima),
                completo = progress.completo ? 1 : 0,
                completed = progress.completo,
                is_completed = progress.completo,
                passed = progress.completo
            };

            string payloadJson = JsonUtility.ToJson(payload);
            Debug.Log($"[Progress][HTTP] SaveProgress payload -> {payloadJson}");

            ApiResponse<ProgressSaveResult> response = await SendProgressSaveWithValidationAsync(
                $"{_baseUrl}/game/slots/{slotNumber}/progress",
                payloadJson,
                progress,
                ct);

            Debug.Log($"[Progress][HTTP] Endpoint result -> endpoint=/game/slots/{{slot}}/progress success={response.success} message={response.message}");

            if (response.success)
            {
                Debug.Log("[Progress][HTTP] SaveProgress used primary endpoint: /game/slots/{slotNumber}/progress");
                return response;
            }

            response = await SendProgressSaveWithValidationAsync(
                $"{_baseUrl}/game/progress/save",
                payloadJson,
                progress,
                ct);

            Debug.Log($"[Progress][HTTP] Endpoint result -> endpoint=/game/progress/save success={response.success} message={response.message}");

            if (response.success)
            {
                Debug.Log("[Progress][HTTP] SaveProgress fallback used: /game/progress/save");
                return response;
            }

            response = await SendProgressSaveWithValidationAsync(
                $"{_baseUrl}/progress/save",
                payloadJson,
                progress,
                ct);

            Debug.Log($"[Progress][HTTP] Endpoint result -> endpoint=/progress/save success={response.success} message={response.message}");
            if (response.success)
            {
                Debug.Log("[Progress][HTTP] SaveProgress fallback used: /progress/save");
            }

            return response;
        }

        private async Task<ApiResponse<ProgressSaveResult>> SendProgressSaveWithValidationAsync(
            string url,
            string jsonPayload,
            ProgressData expected,
            CancellationToken ct)
        {
            try
            {
                Debug.Log($"[Progress][HTTP] POST {url} | payload={jsonPayload}");
                using var req = UnityWebRequestExtensions.CreateJsonRequest(url, UnityWebRequest.kHttpVerbPOST, jsonPayload, _session.Token);
                var body = await req.SendAsync(ct);
                long statusCode = req.responseCode;

                Debug.Log($"[Progress][HTTP] POST {url} -> status={(int)statusCode} body={body}");

                bool bodyExplicitlyFailed = !string.IsNullOrWhiteSpace(body) && body.IndexOf("\"success\":false", StringComparison.OrdinalIgnoreCase) >= 0;
                if (bodyExplicitlyFailed)
                {
                    return ApiResponse<ProgressSaveResult>.Fail("save_progress_error", "Backend respondio success=false.");
                }

                ApiResponse<ProgressSaveResult> envelope = JsonUtility.FromJson<ApiResponse<ProgressSaveResult>>(body);
                if (envelope != null && envelope.success)
                {
                    return envelope;
                }

                ProgressSaveResult direct = JsonUtility.FromJson<ProgressSaveResult>(body);
                if (direct != null)
                {
                    bool accepted = direct.success
                        || direct.id_partida > 0
                        || (direct.id_nivel > 0 && direct.id_nivel == expected.id_nivel)
                        || direct.slot_numero > 0;

                    if (accepted)
                    {
                        return ApiResponse<ProgressSaveResult>.Ok(direct, direct.message ?? "OK");
                    }
                }

                string message = envelope?.message;
                if (string.IsNullOrWhiteSpace(message))
                {
                    message = "Respuesta no valida en guardado de progreso.";
                }

                return ApiResponse<ProgressSaveResult>.Fail("invalid_payload", message);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"GameDataApiClient POST error [{url}]: {ex.Message}");
                return ApiResponse<ProgressSaveResult>.Fail("network_error", ex.Message);
            }
        }

        public Task<ApiResponse<EquippedItemsData>> UpdateEquipmentAsync(int slotNumber, UpdateEquipmentRequest request, CancellationToken ct = default) =>
            SendEnvelopeAsync<EquippedItemsData>($"{_baseUrl}/game/slots/{slotNumber}/equipment", UnityWebRequest.kHttpVerbPUT, request, ct);

        private async Task<ApiResponse<SlotDetailResponse>> GetSlotDetailFromEndpointAsync(string url, CancellationToken ct)
        {
            try
            {
                using var req = UnityWebRequestExtensions.CreateGet(url, _session.Token);
                var body = await req.SendAsync(ct);

                if (EnableHttpDebugLogs)
                {
                    Debug.Log($"[HTTP] GET {url} -> {(int)req.responseCode} | Body: {body}");
                }

                var payload = JsonUtility.FromJson<SlotDetailResponse>(body);
                if (payload == null || !payload.success || payload.slot == null)
                {
                    return ApiResponse<SlotDetailResponse>.Fail("invalid_payload", "No fue posible cargar detalle de slot");
                }

                return ApiResponse<SlotDetailResponse>.Ok(payload);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"GameDataApiClient SlotDetail error [{url}]: {ex.Message}");
                return ApiResponse<SlotDetailResponse>.Fail("network_error", ex.Message);
            }
        }

        private async Task<ApiResponse<T>> SendEnvelopeAsync<T>(string url, string method, object payload, CancellationToken ct)
        {
            try
            {
                var json = JsonUtility.ToJson(payload);

                if (EnableHttpDebugLogs)
                {
                    Debug.Log($"[HTTP] {method} {url} | Request: {json}");
                }

                using var req = UnityWebRequestExtensions.CreateJsonRequest(url, method, json, _session.Token);
                var body = await req.SendAsync(ct);

                if (EnableHttpDebugLogs)
                {
                    Debug.Log($"[HTTP] {method} {url} -> {(int)req.responseCode} | Body: {body}");
                }

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
                Debug.LogWarning($"GameDataApiClient {method} error [{url}]: {ex.Message}");
                return ApiResponse<T>.Fail("network_error", ex.Message);
            }
        }

        private async Task<ApiResponse<bool>> SendNoContentEnvelopeAsync(string url, string method, CancellationToken ct)
        {
            try
            {
                using var req = UnityWebRequestExtensions.CreateJsonRequest(url, method, "{}", _session.Token);
                var body = await req.SendAsync(ct);

                if (EnableHttpDebugLogs)
                {
                    Debug.Log($"[HTTP] {method} {url} -> {(int)req.responseCode} | Body: {body}");
                }

                if (string.IsNullOrWhiteSpace(body))
                {
                    return req.responseCode >= 200 && req.responseCode < 300
                        ? ApiResponse<bool>.Ok(true)
                        : ApiResponse<bool>.Fail("http_error", $"HTTP {(int)req.responseCode}");
                }

                var envelope = JsonUtility.FromJson<ApiResponse<bool>>(body);
                if (envelope != null && envelope.success)
                {
                    return envelope;
                }

                var slotDetailEnvelope = JsonUtility.FromJson<ApiResponse<SlotDetailResponse>>(body);
                if (slotDetailEnvelope != null)
                {
                    return slotDetailEnvelope.success
                        ? ApiResponse<bool>.Ok(true, slotDetailEnvelope.message)
                        : ApiResponse<bool>.Fail(slotDetailEnvelope.error?.code ?? "delete_slot_error", slotDetailEnvelope.message ?? "No fue posible eliminar slot.");
                }

                if (req.responseCode >= 200 && req.responseCode < 300)
                {
                    return ApiResponse<bool>.Ok(true);
                }

                return ApiResponse<bool>.Fail("delete_slot_error", "Respuesta no reconocida del backend al eliminar slot.");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Fail("network_error", ex.Message);
            }
        }

        [Serializable]
        private class SaveProgressCompatRequest
        {
            public int slot_numero;
            public int slotNumber;
            public int id_nivel;
            public int idNivel;
            public int puntuacion_maxima;
            public int puntuacionMaxima;
            public int score;
            public int completo;
            public bool completed;
            public bool is_completed;
            public bool passed;
        }
    }
}