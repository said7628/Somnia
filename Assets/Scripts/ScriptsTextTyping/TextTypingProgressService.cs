using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Somnia.Economy.Core;
using Somnia.Economy.DTOs;
using Somnia.Economy.Services;
using UnityEngine;

public static class TextTypingProgressService
{
    public struct SaveAttemptResult
    {
        public bool Success;
        public int PreviousMaxScore;
        public int NewMaxScore;
        public bool CompletedAfterSave;
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

        var loadResponse = await gameDataService.LoadProgressAsync(Mathf.Max(1, slot));
        if (loadResponse == null || !loadResponse.success)
        {
            Debug.LogWarning($"[TextTypingProgress] LoadProgress failed while reading best. slot={slot} message={(loadResponse != null ? loadResponse.message : "null")}");
            return 0;
        }

        ProgressData existing = loadResponse.data != null
            ? loadResponse.data.FirstOrDefault(p => p != null && p.id_nivel == levelId)
            : null;

        int loadedBest = existing != null ? Mathf.Max(0, existing.puntuacion_maxima) : 0;
        Debug.Log($"[TextTypingProgress] loaded personal best from backend levelId={levelId} slot={Mathf.Max(1, slot)} best={loadedBest}");
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

        var saveResponse = await gameDataService.SaveProgressAsync(validSlot, updatedProgress);
        result.Success = saveResponse != null && saveResponse.success;
        result.CompletedAfterSave = existing.completo;
        Debug.Log($"[TextTypingProgress] persisted personal best save {(result.Success ? "success" : "failure")} levelId={levelId} slot={validSlot} completed={result.CompletedAfterSave} message={(saveResponse != null ? saveResponse.message : "null")}");

        return result;
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
}