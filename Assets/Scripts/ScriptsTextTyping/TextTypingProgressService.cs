using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Somnia.Economy.Core;
using Somnia.Economy.DTOs;
using Somnia.Economy.Services;
using Somnia.UnityClient;
using UnityEngine;
using UnityEngine.Networking;

public static class TextTypingProgressService
{
    public struct SaveAttemptResult
    {
        public bool Success;
        public int PreviousMaxScore;
        public int NewMaxScore;
        public bool CompletedAfterSave;
    }

    public struct ScoreRewardResult
    {
        public bool Success;
        public int AwardedYatzis;
        public int TotalYatzis;
        public float BackendMultiplier;
        public string Message;
    }

    public static async Task<int> LoadPersonalBestAsync(int levelId, int slot)
    {
        if (levelId <= 0)
        {
            return 0;
        }

        GameDataService gameDataService = await ResolveGameDataServiceAsync();
        if (gameDataService == null)
        {
            Debug.LogWarning("[TextTypingProgress] Cannot load personal best. GameDataService not available.");
            return 0;
        }

        int validSlot = Mathf.Max(1, slot);
        Debug.Log($"[TextTypingProgress] LoadPersonalBest start slot={validSlot} levelId={levelId}");
        var loadResponse = await gameDataService.LoadProgressAsync(validSlot);
        if (loadResponse == null || !loadResponse.success)
        {
            Debug.LogWarning($"[TextTypingProgress] LoadProgress failed while reading best. slot={slot} message={(loadResponse != null ? loadResponse.message : "null")}");
            return 0;
        }

        ProgressData existing = loadResponse.data != null
            ? loadResponse.data.FirstOrDefault(p => p != null && p.id_nivel == levelId)
            : null;

        int loadedBest = existing != null ? Mathf.Max(0, existing.puntuacion_maxima) : 0;
        int count = loadResponse.data != null ? loadResponse.data.Count : 0;
        Debug.Log($"[TextTypingProgress] loaded personal best from backend levelId={levelId} slot={validSlot} best={loadedBest} entries={count}");
        return loadedBest;
    }

    public static async Task<SaveAttemptResult> SaveAttemptAsync(int levelId, int scoreFinal, bool passed, int slot)
    {
        SaveAttemptResult result = new SaveAttemptResult
        {
            Success = false,
            PreviousMaxScore = 0,
            NewMaxScore = Mathf.Max(0, scoreFinal),
            CompletedAfterSave = passed
        };

        if (levelId <= 0)
        {
            Debug.LogWarning("[TextTypingProgress] Cannot save attempt. Invalid levelId.");
            return result;
        }

        GameDataService gameDataService = await ResolveGameDataServiceAsync();
        if (gameDataService == null)
        {
            Debug.LogWarning("[TextTypingProgress] Cannot save attempt. GameDataService not available.");
            return result;
        }

        int validSlot = Mathf.Max(1, slot);
        Debug.Log($"[TextTypingProgress] SaveAttempt load-before-save slot={validSlot} levelId={levelId}");
        var loadResponse = await gameDataService.LoadProgressAsync(validSlot);
        if (loadResponse == null || !loadResponse.success)
        {
            Debug.LogWarning($"[TextTypingProgress] LoadProgress failed before save. slot={validSlot} message={(loadResponse != null ? loadResponse.message : "null")}");
            return result;
        }

        List<ProgressData> updatedProgress = (loadResponse.data ?? Array.Empty<ProgressData>()).Select(p => new ProgressData
        {
            id_nivel = p.id_nivel,
            completo = p.completo,
            puntuacion_maxima = p.puntuacion_maxima,
            ultimo_intento = p.ultimo_intento
        }).ToList();

        ProgressData existing = updatedProgress.FirstOrDefault(p => p.id_nivel == levelId);
        if (existing == null)
        {
            existing = new ProgressData
            {
                id_nivel = levelId,
                completo = false,
                puntuacion_maxima = 0
            };
            updatedProgress.Add(existing);
        }

        result.PreviousMaxScore = Mathf.Max(0, existing.puntuacion_maxima);
        result.NewMaxScore = Mathf.Max(result.PreviousMaxScore, Mathf.Max(0, scoreFinal));
        Debug.Log($"[TextTypingProgress] previous personal best={result.PreviousMaxScore} current run score={Mathf.Max(0, scoreFinal)} new personal best={result.NewMaxScore} levelId={levelId} slot={validSlot}");

        existing.completo = existing.completo || passed;
        existing.puntuacion_maxima = result.NewMaxScore;
        existing.ultimo_intento = DateTime.UtcNow;

        Debug.Log($"[Progress] Saving -> slot={validSlot} levelId={levelId} score={existing.puntuacion_maxima} completo={(existing.completo ? 1 : 0)}");

        var saveResponse = await gameDataService.SaveProgressAsync(validSlot, updatedProgress);
        result.Success = saveResponse != null && saveResponse.success;
        result.CompletedAfterSave = existing.completo;
        Debug.Log($"[Progress] Save response -> success={result.Success} message={(saveResponse != null ? saveResponse.message : "null")} levelId={levelId} slot={validSlot} score={existing.puntuacion_maxima} completo={(result.CompletedAfterSave ? 1 : 0)}");
        Debug.Log($"[TextTypingProgress] persisted personal best save {(result.Success ? "success" : "failure")} levelId={levelId} slot={validSlot} completed={result.CompletedAfterSave} message={(saveResponse != null ? saveResponse.message : "null")}");

        var verifyResponse = await gameDataService.LoadProgressAsync(validSlot);
        int verifyCount = verifyResponse != null && verifyResponse.data != null ? verifyResponse.data.Count : 0;
        bool existsAfterSave = verifyResponse != null
            && verifyResponse.success
            && verifyResponse.data != null
            && verifyResponse.data.Any(p => p != null && p.id_nivel == levelId);
        string ids = verifyResponse != null && verifyResponse.data != null
            ? string.Join(",", verifyResponse.data.Where(p => p != null).Select(p => p.id_nivel))
            : "<none>";
        Debug.Log($"[TextTypingProgress] verify-after-save slot={validSlot} entries={verifyCount} levelIds=[{ids}] hasLevel={existsAfterSave} targetLevel={levelId}");

        return result;
    }

    public static async Task<ScoreRewardResult> SaveScoreAndRewardAsync(int levelId, int score)
    {
        ScoreRewardResult result = new ScoreRewardResult
        {
            Success = false,
            AwardedYatzis = 0,
            TotalYatzis = 0,
            BackendMultiplier = 0f,
            Message = ""
        };

        if (levelId <= 0)
        {
            result.Message = "id_nivel inválido.";
            return result;
        }

        string token = GameSessionManager.Instance != null ? GameSessionManager.Instance.AccessToken : null;
        if (string.IsNullOrWhiteSpace(token))
        {
            result.Message = "No hay token de sesión para guardar score en backend.";
            Debug.LogWarning($"[TextTypingProgress] {result.Message}");
            return result;
        }

        ApiConfig runtimeConfig = ScriptableObject.CreateInstance<ApiConfig>();
        string url = runtimeConfig.BuildUrl("/progress/score");

        ScorePayload payload = new ScorePayload
        {
            id_nivel = levelId,
            score = Mathf.Max(0, score)
        };

        string jsonPayload = JsonUtility.ToJson(payload);

        try
        {
            using UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {token}");
            request.timeout = runtimeConfig.TimeoutSeconds;

            Debug.Log($"[TextTypingProgress] POST {url} payload={jsonPayload}");

            var operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                await Task.Yield();
            }

            string body = request.downloadHandler != null ? request.downloadHandler.text : "";
            Debug.Log($"[TextTypingProgress] POST {url} -> {(int)request.responseCode} body={body}");

            if (request.result != UnityWebRequest.Result.Success)
            {
                result.Message = $"HTTP {(int)request.responseCode}: {request.error}";
                return result;
            }

            ScoreRewardEnvelope envelope = JsonUtility.FromJson<ScoreRewardEnvelope>(body);
            if (envelope == null || !envelope.success || envelope.reward == null)
            {
                result.Message = "Respuesta inválida del endpoint /progress/score.";
                return result;
            }

            result.Success = true;
            result.AwardedYatzis = Mathf.Max(0, envelope.reward.yatzis_ganados);
            result.TotalYatzis = Mathf.Max(0, envelope.reward.yatzis_total);
            result.BackendMultiplier = envelope.reward.multiplicador;
            result.Message = envelope.message;
            return result;
        }
        catch (Exception ex)
        {
            result.Message = ex.Message;
            Debug.LogWarning($"[TextTypingProgress] SaveScoreAndReward exception: {ex.Message}");
            return result;
        }
    }

    private static async Task<GameDataService> ResolveGameDataServiceAsync()
    {
        EconomyModule module = EconomyModule.Instance ?? UnityEngine.Object.FindFirstObjectByType<EconomyModule>(FindObjectsInactive.Include);
        if (module == null)
        {
            module = new GameObject("[EconomyModule]").AddComponent<EconomyModule>();
        }

        const float timeoutSeconds = 5f;
        float elapsed = 0f;

        while (module != null && module.GameDataService == null && elapsed < timeoutSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            await Task.Yield();
        }

        return module != null ? module.GameDataService as GameDataService : null;
    }

    [Serializable]
    private class ScorePayload
    {
        public int id_nivel;
        public int score;
    }

    [Serializable]
    private class ScoreRewardEnvelope
    {
        public bool success;
        public string message;
        public ScoreRewardData reward;
    }

    [Serializable]
    private class ScoreRewardData
    {
        public int yatzis_ganados;
        public int yatzis_total;
        public float multiplicador;
    }
}