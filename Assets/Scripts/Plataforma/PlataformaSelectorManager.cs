using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Somnia.Economy.Core;
using Somnia.Economy.Services;
using Somnia.UnityClient;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlataformaSelectorManager : MonoBehaviour
{
    [Serializable]
    public class PlataformaSublevelConfig
    {
        public int levelId;
        public string sceneName;
        public Button button;
        public int requiredPreviousLevelId;

        [NonSerialized] public bool isUnlocked;
        [NonSerialized] public bool isCompleted;
    }

    [Header("Selector State Canvases")]
    [SerializeField] private GameObject plataforma;
    [SerializeField] private GameObject plataforma1a2;
    [SerializeField] private GameObject plataforma2a3;
    [SerializeField] private GameObject plataforma3a4;
    [SerializeField] private GameObject plataforma4a5;

    [Header("Buttons")]
    [SerializeField] private Button tacha1Button;
    [SerializeField] private Button tacha2Button;
    [SerializeField] private Button tacha3Button;
    [SerializeField] private Button tacha4Button;
    [SerializeField] private Button tacha5Button;

    [Header("Levels")]
    [SerializeField] private PlataformaSublevelConfig[] levels;

    [SerializeField] private ToastMessage toastMessage;

    private readonly HashSet<int> completedLevelIds = new HashSet<int>();
    private GameDataService gameDataService;
    private int currentSlot = -1;

    private void Awake()
    {
        Debug.Log($"[PlataformaSelector] Awake scene={SceneManager.GetActiveScene().name}");
        EnsureDefaultLevelConfig();
        EnsureToastReference();
    }

    private async void Start()
    {
        currentSlot = ResolveCurrentSlot();
        if (currentSlot <= 0)
        {
            Debug.LogError("[PlataformaSelector][ERROR] Could not resolve current slot");
            return;
        }

        Debug.Log($"[PlataformaSelector] Current slot resolved={currentSlot}");

        bool resolved = await ResolveGameDataServiceAsync();
        if (!resolved)
        {
            ConfigureStateAndButtons();
            return;
        }

        Debug.Log($"[PlataformaSelector] Loading progress for slot={currentSlot}");
        await LoadProgressAsync();
        ConfigureStateAndButtons();
    }

    private int ResolveCurrentSlot()
    {
        if (GameSessionManager.Instance != null)
        {
            return Mathf.Max(1, GameSessionManager.Instance.CurrentSlotNumber);
        }

        if (TextTypingSession.SlotNumber > 0)
        {
            return TextTypingSession.SlotNumber;
        }

        return -1;
    }

    private async Task<bool> ResolveGameDataServiceAsync()
    {
        if (gameDataService != null)
        {
            return true;
        }

        EconomyModule module = EconomyModule.Instance ?? FindFirstObjectByType<EconomyModule>(FindObjectsInactive.Include);
        if (module == null)
        {
            module = new GameObject("[EconomyModule]").AddComponent<EconomyModule>();
        }

        const float timeoutSeconds = 6f;
        float elapsed = 0f;
        while (module != null && module.GameDataService == null && elapsed < timeoutSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            await Task.Yield();
        }

        gameDataService = module != null ? module.GameDataService as GameDataService : null;
        return gameDataService != null;
    }

    private async Task LoadProgressAsync()
    {
        completedLevelIds.Clear();

        var response = await gameDataService.GetSlotDetailAsync(currentSlot);
        if (response == null || !response.success || response.data == null)
        {
            Debug.LogWarning("[PlataformaSelector][WARN] Slot detail unavailable. Using empty progress fallback.");
            LogCompletedLevels();
            return;
        }

        ProgresoDto[] progressRows = response.data.progreso ?? response.data.progress;
        if (progressRows != null)
        {
            for (int i = 0; i < progressRows.Length; i++)
            {
                ProgresoDto row = progressRows[i];
                if (row == null)
                {
                    continue;
                }

                if (row.completo == 1)
                {
                    completedLevelIds.Add(row.id_nivel);
                }
            }
        }

        LogCompletedLevels();
    }

    private void LogCompletedLevels()
    {
        string ids = completedLevelIds.Count == 0 ? "" : string.Join(",", completedLevelIds.OrderBy(x => x));
        Debug.Log($"[PlataformaSelector] Completed level ids=[{ids}]");
    }

    private void ConfigureStateAndButtons()
    {
        UpdateLevelFlags();
        ActivateSelectorState();
        BindButtons();
    }

    private void UpdateLevelFlags()
    {
        foreach (PlataformaSublevelConfig level in levels)
        {
            if (level == null)
            {
                continue;
            }

            level.isCompleted = completedLevelIds.Contains(level.levelId);
            level.isUnlocked = level.requiredPreviousLevelId <= 0 || completedLevelIds.Contains(level.requiredPreviousLevelId);
            Debug.Log($"[PlataformaSelector] Level id={level.levelId} scene={level.sceneName} unlocked={level.isUnlocked.ToString().ToLowerInvariant()} completed={level.isCompleted.ToString().ToLowerInvariant()}");
        }
    }

    private void ActivateSelectorState()
    {
        GameObject[] canvases = { plataforma, plataforma1a2, plataforma2a3, plataforma3a4, plataforma4a5 };
        string[] canvasNames = { "Plataforma", "Plataforma1a2", "Plataforma2a3", "Plataforma3a4", "Plataforma4a5" };

        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] == null)
            {
                Debug.LogError($"[PlataformaSelector][ERROR] Missing selector canvas reference: {canvasNames[i]}");
            }
        }

        int stateIndex = 0;
        if (completedLevelIds.Contains(7)) stateIndex = 1;
        if (completedLevelIds.Contains(8)) stateIndex = 2;
        if (completedLevelIds.Contains(9)) stateIndex = 3;
        if (completedLevelIds.Contains(10)) stateIndex = 4;

        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] != null)
            {
                canvases[i].SetActive(i == stateIndex);
            }
        }

        Debug.Log($"[PlataformaSelector] State index={stateIndex} activeCanvas={canvasNames[stateIndex]}");
    }

    private void BindButtons()
    {
        for (int i = 0; i < levels.Length; i++)
        {
            PlataformaSublevelConfig level = levels[i];
            if (level == null || level.button == null)
            {
                string fallbackName = $"Tacha{i + 1}";
                Debug.LogError($"[PlataformaSelector][ERROR] Missing button reference: {fallbackName}");
                continue;
            }

            level.button.onClick.RemoveAllListeners();
            PlataformaSublevelConfig localLevel = level;
            level.button.onClick.AddListener(() => OnLevelButtonClicked(localLevel));
        }
    }

    private void OnLevelButtonClicked(PlataformaSublevelConfig level)
    {
        Debug.Log($"[PlataformaSelector] Button clicked levelId={level.levelId} scene={level.sceneName}");

        if (!level.isUnlocked)
        {
            Debug.LogWarning($"[PlataformaSelector][WARN] Level locked id={level.levelId}");
            if (toastMessage != null)
            {
                toastMessage.Show("Nivel bloqueado. Completa el anterior primero.");
            }
            return;
        }

        PlataformaSession.SlotNumber = currentSlot;
        PlataformaSession.LevelId = level.levelId;
        PlataformaSession.SourceLevelScene = level.sceneName;
        PlataformaSession.ReturnScene = "Plataforma";
        PlataformaSession.Completed = false;
        PlataformaSession.RewardAlreadySaved = false;

        TextTypingSession.SlotNumber = currentSlot;
        TextTypingSession.LevelId = level.levelId;
        TextTypingSession.LevelName = level.sceneName;
        TextTypingSession.LastLevelId = level.levelId;
        TextTypingSession.LastLevelSceneName = level.sceneName;

        Debug.Log($"[PlataformaSelector] Loading scene={level.sceneName} levelId={level.levelId} slot={currentSlot}");
        SceneManager.LoadScene(level.sceneName);
    }

    private void EnsureDefaultLevelConfig()
    {
        if (levels != null && levels.Length == 5 && levels.All(x => x != null && x.levelId > 0 && !string.IsNullOrWhiteSpace(x.sceneName)))
        {
            if (levels[0].button == null) levels[0].button = tacha1Button;
            if (levels[1].button == null) levels[1].button = tacha2Button;
            if (levels[2].button == null) levels[2].button = tacha3Button;
            if (levels[3].button == null) levels[3].button = tacha4Button;
            if (levels[4].button == null) levels[4].button = tacha5Button;
            return;
        }

        levels = new[]
        {
            new PlataformaSublevelConfig { levelId = 7, sceneName = "Plataforma1", button = tacha1Button, requiredPreviousLevelId = 0 },
            new PlataformaSublevelConfig { levelId = 8, sceneName = "Plataforma2", button = tacha2Button, requiredPreviousLevelId = 7 },
            new PlataformaSublevelConfig { levelId = 9, sceneName = "Plataforma3", button = tacha3Button, requiredPreviousLevelId = 8 },
            new PlataformaSublevelConfig { levelId = 10, sceneName = "Plataforma4", button = tacha4Button, requiredPreviousLevelId = 9 },
            new PlataformaSublevelConfig { levelId = 11, sceneName = "Plataforma5", button = tacha5Button, requiredPreviousLevelId = 10 }
        };
    }

    private void EnsureToastReference()
    {
        if (toastMessage == null)
        {
            toastMessage = FindFirstObjectByType<ToastMessage>(FindObjectsInactive.Include);
        }
    }
}