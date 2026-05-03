using System;
using System.Threading.Tasks;
using Somnia.Economy.Core;
using Somnia.Economy.DTOs;
using Somnia.Economy.Services;
using UnityEngine;

public static class PlataformaProgressService
{
    public static async Task<ProgressSaveResult> SaveCompletionRewardAsync(int slotNumber, int levelId, int totalYatzisEarned)
    {
        int validSlot = Mathf.Max(1, slotNumber);
        int validLevelId = Mathf.Max(1, levelId);
        int reward = totalYatzisEarned >= 60 ? 60 : 50;

        GameDataService gameDataService = await ResolveGameDataServiceAsync();
        if (gameDataService == null)
        {
            throw new Exception("GameDataService not available.");
        }

        ProgressData progress = new ProgressData
        {
            id_nivel = validLevelId,
            puntuacion_maxima = reward,
            completo = true,
            ultimo_intento = DateTime.UtcNow,
            delta_yatzis = reward,
            reward_yatzis = reward
        };

        Debug.Log($"[ScorePlataformaProgress] Save payload includes delta_yatzis={progress.delta_yatzis} rewardYatzis={progress.reward_yatzis}");

        var saveResponse = await gameDataService.SaveProgressEntryAsync(validSlot, progress);
        ProgressSaveResult result = saveResponse != null ? saveResponse.data : null;
        Debug.Log($"[ScorePlataformaProgress] Save response success={(saveResponse != null && saveResponse.success)} yatzis={(result != null ? result.yatzis : 0)}");

        if (saveResponse == null || !saveResponse.success || result == null)
        {
            throw new Exception(saveResponse != null ? saveResponse.message : "null response");
        }

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