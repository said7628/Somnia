using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Somnia.Economy.Core;
using Somnia.Economy.DTOs;
using Somnia.Economy.Interfaces;
using Somnia.Economy.Services;
using Somnia.UnityClient;
using System.Linq;
using TMPro;
using UnityEngine.UI;


public class IslandLevelManager : MonoBehaviour
{
    [Serializable]
    public class LevelEntry
    {
        [Header("ID del nivel en la tabla niveles")]
        public int dbLevelId;

        [Header("Escena que se debe cargar")]
        public string sceneName;

        [Header("Opcional: objeto visual del nivel")]
        public GameObject levelRoot;

        [Header("Opcional: visual de bloqueado")]
        public GameObject blockedVisual;

        [Header("Opcional: visual de desbloqueado")]
        public GameObject unlockedVisual;

        [Header("Opcional: visual de jugado/completado")]
        public GameObject playedVisual;

        [NonSerialized] public bool isUnlocked;
        [NonSerialized] public bool isPlayed;
        [NonSerialized] public bool isCompleted;
    }

    public static IslandLevelManager Instance { get; private set; }

    [Header("Niveles configurados en orden")]
    [SerializeField] private LevelEntry[] levels;

    [Header("Config")]
    [SerializeField] private bool lockLevelsWithoutScene = true;
    [SerializeField] private bool verboseLogs = true;
    [SerializeField] private ToastMessage toastMessage;
    [Header("UI Yatzis (Isla1)")]
    [SerializeField] private TextMeshProUGUI yatzisTotalText;
    [SerializeField] private string yatzisTextObjectName = "Yatzis";

    private GameDataService gameDataService;
    private int currentSlot = 1;
    private int currentTotalYatzis = 0;
    private bool isLoading;
    private bool isInitialized;
    private EconomyModule economyModule;

    private readonly HashSet<int> playedLevels = new HashSet<int>();
    private readonly HashSet<int> completedLevels = new HashSet<int>();
    private const string RuntimeToastName = "RuntimeToast_Isla1";

    public bool IsInitialized
    {
        get { return isInitialized; }
    }

    public bool IsBusy
    {
        get { return isLoading; }
    }

    public int CurrentTotalYatzis
    {
        get { return Mathf.Max(0, currentTotalYatzis); }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private async void Start()
    {
        currentSlot = ResolveCurrentSlot();
        PersistCurrentIslandScene();
        Log("Current slot resolved for island flow=" + currentSlot);
        Log("Active scene on island start=" + SceneManager.GetActiveScene().name);
        TryResolveToast();
        RefreshYatzisText();

        bool resolved = await ResolveGameDataServiceWithRecoveryAsync();
        if (!resolved)
        {
            Debug.LogError("IslandLevelManager: no se pudo resolver GameDataService. Se aplican reglas fallback.");
            ShowToast("No se pudo cargar progreso. Modo local.");
            ApplyFallbackRules();
            return;
        }

        await LoadStateAsync();
    }

    private int ResolveCurrentSlot()
    {
        if (GameSessionManager.Instance != null)
        {
            return Mathf.Max(1, GameSessionManager.Instance.CurrentSlotNumber);
        }

        return 1;
    }

    private async Task<bool> ResolveGameDataServiceWithRecoveryAsync()
    {
        if (gameDataService != null)
        {
            return true;
        }

        if (economyModule == null)
        {
            economyModule = EconomyModule.Instance ?? FindFirstObjectByType<EconomyModule>(FindObjectsInactive.Include);
            if (economyModule != null)
            {
                Log("EconomyModule autodescubierto en escena.");
            }
        }

        if (TryResolveFromModule(economyModule))
        {
            Log("GameDataService resuelto desde EconomyModule existente.");
            return true;
        }

        economyModule = EconomyModule.Instance ?? FindFirstObjectByType<EconomyModule>(FindObjectsInactive.Include);
        if (economyModule == null)
        {
            economyModule = new GameObject("[EconomyModule]").AddComponent<EconomyModule>();
            Log("Se creo EconomyModule runtime para recuperar GameDataService.");
        }

        const float timeoutSeconds = 6f;
        float elapsed = 0f;
        while (economyModule != null && economyModule.GameDataService == null && elapsed < timeoutSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            await Task.Yield();
        }

        if (TryResolveFromModule(economyModule))
        {
            Log("GameDataService resuelto desde EconomyModule runtime.");
            return true;
        }

        Debug.LogError("IslandLevelManager: no se pudo resolver GameDataService desde ninguna fuente.");
        return false;
    }

    public async Task LoadStateAsync()
    {
        if (isLoading)
        {
            return;
        }

        isLoading = true;
        isInitialized = false;

        try
        {
            playedLevels.Clear();
            completedLevels.Clear();

            var response = await gameDataService.GetSlotDetailAsync(currentSlot);

            if (response == null)
            {
                Debug.LogWarning("IslandLevelManager: GetSlotDetailAsync devolvio null.");
                ApplyFallbackRules();
                return;
            }

            if (!response.success || response.data == null)
            {
                Debug.LogWarning(
                    "IslandLevelManager: no se pudo cargar detalle del slot. " +
                    (response != null ? response.message : "respuesta nula")
                );
                ApplyFallbackRules();
                return;
            }

            currentTotalYatzis = Mathf.Max(0, response.data.slot != null ? response.data.slot.yatzis : 0);
            Log("Loaded slot yatzis total=" + currentTotalYatzis + " slot=" + currentSlot);
            RefreshYatzisText();

            ProgresoDto[] progreso = response.data.progreso ?? response.data.progress;
            Log("GetSlotDetailAsync response success=" + response.success + " slot=" + currentSlot + " progreso_count=" + (progreso != null ? progreso.Length : 0));

            if (progreso != null)
            {
                for (int i = 0; i < progreso.Length; i++)
                {
                    ProgresoDto p = progreso[i];
                    if (p == null) continue;

                    playedLevels.Add(p.id_nivel);

                    if (p.completo == 1)
                    {
                        completedLevels.Add(p.id_nivel);
                    }
                }
            }

            if (progreso != null && progreso.Length > 0)
            {
                string levelIds = string.Join(",", Array.ConvertAll(progreso, p => p != null ? p.id_nivel.ToString() : "null"));
                Log("GetSlotDetailAsync progress level ids=[" + levelIds + "]");
            }

            Log("loaded progress for level 1 -> completed=" + completedLevels.Contains(1));

            ApplyUnlockRules();
            RefreshVisuals();

            isInitialized = true;
            Log("Estado del mapa cargado correctamente.");
        }
        catch (Exception ex)
        {
            Debug.LogError("IslandLevelManager: error cargando progreso: " + ex.Message);
            ApplyFallbackRules();
        }
        finally
        {
            isLoading = false;
        }
    }

    public bool CanEnterLevel(int dbLevelId)
    {
        LevelEntry level = GetLevel(dbLevelId);
        return level != null && level.isUnlocked;
    }

    public bool HasPlayedLevel(int dbLevelId)
    {
        return playedLevels.Contains(dbLevelId);
    }

    public bool IsCompletedLevel(int dbLevelId)
    {
        return completedLevels.Contains(dbLevelId);
    }

    public async void TryEnterLevel(int dbLevelId)
    {
        if (!isInitialized)
        {
            Log("Todavia no termina de inicializar.");
            ShowToast("Cargando progreso de isla...");
            return;
        }

        if (isLoading)
        {
            Log("Manager ocupado.");
            ShowToast("Espera un momento...");
            return;
        }

        LevelEntry level = GetLevel(dbLevelId);

        if (level == null)
        {
            Debug.LogError("IslandLevelManager: no existe configuracion para dbLevelId=" + dbLevelId);
            return;
        }

        if (!level.isUnlocked)
        {
            Log("blocked level attempted -> levelId=" + dbLevelId);
            ShowToast("Nivel bloqueado, completa el nivel anterior para acceder a este.");
            return;
        }

        if (string.IsNullOrWhiteSpace(level.sceneName))
        {
            Log("Nivel " + dbLevelId + " sin escena asignada. Se queda bloqueado.");
            ShowToast("Este nivel aun no esta disponible.");
            return;
        }

        await MarkLevelAsPlayedAsync(dbLevelId);
        PersistCurrentIslandScene();

        TextTypingSession.LevelId = dbLevelId;
        TextTypingSession.LevelName = level.sceneName;
        Log("Entering level -> slot=" + currentSlot + " levelId=" + dbLevelId + " scene=" + level.sceneName);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Vector3 returnPosition = player != null ? player.transform.position : Vector3.zero;
        Quaternion returnRotation = player != null ? player.transform.rotation : Quaternion.identity;
        string sourceIslandScene = SceneManager.GetActiveScene().name;

        TextTypingSession.TrackReturnContext(sourceIslandScene, returnPosition, returnRotation);

        string normalizedScene = NormalizeSceneName(level.sceneName);
        Log("Cargando escena: " + normalizedScene + " with dbLevelId=" + dbLevelId);
        Log("Source island scene=" + sourceIslandScene + " return scene=" + TextTypingSession.ResolveReturnScene() + " return position=" + returnPosition);
        SceneManager.LoadScene(normalizedScene);
    }

    private void PersistCurrentIslandScene()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrWhiteSpace(scene))
        {
            GameSessionManager.Instance?.SetCurrentIslandSceneForSlot(currentSlot, scene);
        }
    }

    private async Task MarkLevelAsPlayedAsync(int dbLevelId)
    {
        if (playedLevels.Contains(dbLevelId))
        {
            return;
        }

        if (gameDataService == null)
        {
            Debug.LogWarning("IslandLevelManager: GameDataService null al marcar nivel jugado. Se aplica fallback local.");
            playedLevels.Add(dbLevelId);
            ApplyUnlockRules();
            RefreshVisuals();
            return;
        }

        try
        {
            isLoading = true;

            var progressResponse = await gameDataService.LoadProgressAsync(currentSlot);

            List<ProgressData> updatedProgress = new List<ProgressData>();

            if (progressResponse != null && progressResponse.success && progressResponse.data != null)
            {
                foreach (ProgressData item in progressResponse.data)
                {
                    if (item == null) continue;

                    updatedProgress.Add(new ProgressData
                    {
                        id_nivel = item.id_nivel,
                        completo = item.completo,
                        puntuacion_maxima = item.puntuacion_maxima
                    });
                }
            }

            bool alreadyExists = false;

            for (int i = 0; i < updatedProgress.Count; i++)
            {
                if (updatedProgress[i].id_nivel == dbLevelId)
                {
                    alreadyExists = true;
                    break;
                }
            }

            if (!alreadyExists)
            {
                updatedProgress.Add(new ProgressData
                {
                    id_nivel = dbLevelId,
                    completo = false,
                    puntuacion_maxima = 0
                });
            }

            Debug.Log("[Progress] Saving -> slot=" + currentSlot + " levelId=" + dbLevelId + " score=0 completo=0");
            var saveResponse = await gameDataService.SaveProgressAsync(currentSlot, updatedProgress);
            Debug.Log("[Progress] Save response -> success=" + (saveResponse != null && saveResponse.success) + " message=" + (saveResponse != null ? saveResponse.message : "null") + " levelId=" + dbLevelId + " slot=" + currentSlot + " score=0 completo=0");

            if (saveResponse == null || !saveResponse.success)
            {
                Debug.LogWarning(
                    "IslandLevelManager: SaveProgressAsync fallo para nivel " +
                    dbLevelId + ". " +
                    (saveResponse != null ? saveResponse.message : "respuesta nula")
                );
            }

            var verify = await gameDataService.LoadProgressAsync(currentSlot);
            int verifyCount = verify != null && verify.data != null ? verify.data.Count : 0;
            string ids = verify != null && verify.data != null
                ? string.Join(",", verify.data.Select(x => x != null ? x.id_nivel.ToString() : "null"))
                : "<none>";
            bool hasLevel = verify != null && verify.success && verify.data != null && verify.data.Any(x => x != null && x.id_nivel == dbLevelId);
            Log("MarkLevelAsPlayed verify reload -> entries=" + verifyCount + " levelIds=[" + ids + "] hasLevel=" + hasLevel + " targetLevel=" + dbLevelId);

            playedLevels.Add(dbLevelId);
            ApplyUnlockRules();
            RefreshVisuals();
        }
        catch (Exception ex)
        {
            Debug.LogError("IslandLevelManager: error guardando progreso: " + ex.Message);

            // Para no frenar el flujo, lo marcamos localmente
            playedLevels.Add(dbLevelId);
            ApplyUnlockRules();
            RefreshVisuals();
        }
        finally
        {
            isLoading = false;
        }
    }

    private void ApplyFallbackRules()
    {
        playedLevels.Clear();
        completedLevels.Clear();
        currentTotalYatzis = Mathf.Max(0, Memoria_Islas.misMonedas);
        RefreshYatzisText();

        ApplyUnlockRules();
        RefreshVisuals();

        isInitialized = true;
    }

    private void ApplyUnlockRules()
    {
        if (levels == null || levels.Length == 0)
        {
            return;
        }

        for (int i = 0; i < levels.Length; i++)
        {
            LevelEntry level = levels[i];
            if (level == null) continue;

            level.isPlayed = playedLevels.Contains(level.dbLevelId);
            level.isCompleted = completedLevels.Contains(level.dbLevelId);
            level.isUnlocked = false;
        }

        // Logica especifica que me pediste:
        // Nivel 1 siempre desbloqueado
        LevelEntry level1 = GetLevel(1);
        if (level1 == null && levels.Length > 0)
        {
            level1 = levels[0];
        }

        if (level1 != null)
        {
            level1.isUnlocked = true;
        }

        // Nivel 2 solo si el nivel 1 fue aprobado
        LevelEntry level2 = GetLevel(2);
        if (level2 == null && levels.Length > 1)
        {
            level2 = levels[1];
        }

        if (level2 != null && level1 != null)
        {
            bool level1Completed = completedLevels.Contains(level1.dbLevelId);
            level2.isUnlocked = level1Completed;
            Log("unlock evaluation for level 2 -> level1Id=" + level1.dbLevelId + " completed=" + level1Completed);
            Log("final locked/unlocked state for level 2 -> " + (level2.isUnlocked ? "UNLOCKED" : "LOCKED"));
        }

        // Nivel 3 bloqueado por ahora
        if (levels.Length > 2 && levels[2] != null)
        {
            levels[2].isUnlocked = false;
        }

        if (lockLevelsWithoutScene)
        {
            for (int i = 0; i < levels.Length; i++)
            {
                LevelEntry level = levels[i];
                if (level == null) continue;

                if (string.IsNullOrWhiteSpace(level.sceneName))
                {
                    level.isUnlocked = false;
                }
            }
        }
    }

    private void RefreshVisuals()
    {
        if (levels == null)
        {
            return;
        }

        for (int i = 0; i < levels.Length; i++)
        {
            LevelEntry level = levels[i];
            if (level == null) continue;

            if (level.blockedVisual != null)
            {
                level.blockedVisual.SetActive(!level.isUnlocked);
            }

            if (level.unlockedVisual != null)
            {
                level.unlockedVisual.SetActive(level.isUnlocked && !level.isPlayed);
            }

            if (level.playedVisual != null)
            {
                level.playedVisual.SetActive(level.isPlayed || level.isCompleted);
            }
        }
    }

    private LevelEntry GetLevel(int dbLevelId)
    {
        if (levels == null)
        {
            return null;
        }

        for (int i = 0; i < levels.Length; i++)
        {
            LevelEntry level = levels[i];
            if (level != null && level.dbLevelId == dbLevelId)
            {
                return level;
            }
        }

        return null;
    }

    private void Log(string msg)
    {
        if (verboseLogs)
        {
            Debug.Log("[IslandLevelManager] " + msg);
        }
    }

    private void ShowToast(string message)
    {
        TryResolveToast();
        Debug.Log("[IslandLevelManager.Toast] " + message);
        if (toastMessage != null)
        {
            toastMessage.Show(message);
        }
        else
        {
            Debug.LogWarning("IslandLevelManager: no se pudo mostrar toast visual en Isla1.");
        }
        Log("toast shown for blocked level");
    }

    private void TryResolveToast()
    {
        if (toastMessage == null)
        {
            toastMessage = FindFirstObjectByType<ToastMessage>(FindObjectsInactive.Include);
            if (toastMessage != null)
            {
                Log("ToastMessage encontrado automaticamente.");
            }
        }

        if (toastMessage == null)
        {
            toastMessage = CreateRuntimeToastIfNeeded();
            if (toastMessage != null)
            {
                Log("ToastMessage runtime creado para Canvas (Isla1).");
            }
        }
    }

    private ToastMessage CreateRuntimeToastIfNeeded()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("IslandLevelManager: no se encontro Canvas para crear runtime toast.");
            return null;
        }

        Transform existing = canvas.transform.Find(RuntimeToastName);
        if (existing != null)
        {
            return existing.GetComponent<ToastMessage>();
        }

        GameObject toastRoot = new GameObject(RuntimeToastName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ToastMessage));
        toastRoot.transform.SetParent(canvas.transform, false);

        RectTransform rootRect = toastRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0f);
        rootRect.anchorMax = new Vector2(0.5f, 0f);
        rootRect.pivot = new Vector2(0.5f, 0f);
        rootRect.anchoredPosition = new Vector2(0f, 64f);
        rootRect.sizeDelta = new Vector2(980f, 170f);

        Image background = toastRoot.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.86f);
        background.raycastTarget = false;

        GameObject textGO = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(toastRoot.transform, false);

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(40f, 24f);
        textRect.offsetMax = new Vector2(-40f, -24f);

        TextMeshProUGUI tmp = textGO.GetComponent<TextMeshProUGUI>();
        tmp.text = string.Empty;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.enableWordWrapping = true;
        tmp.fontSize = 42f;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        toastRoot.SetActive(false);
        return toastRoot.GetComponent<ToastMessage>();
    }

    private void RefreshYatzisText()
    {
        TryResolveYatzisText();
        if (yatzisTotalText == null)
        {
            Log("No se pudo resolver TextMeshProUGUI para Yatzis en Isla1.");
            return;
        }

        yatzisTotalText.text = CurrentTotalYatzis.ToString();
        Log("UI Yatzis actualizada en Isla1 -> " + yatzisTotalText.text);
    }

    private void TryResolveYatzisText()
    {
        if (yatzisTotalText != null)
        {
            return;
        }

        TextMeshProUGUI[] candidates = FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < candidates.Length; i++)
        {
            TextMeshProUGUI candidate = candidates[i];
            if (candidate == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(yatzisTextObjectName)
                && string.Equals(candidate.gameObject.name, yatzisTextObjectName, StringComparison.OrdinalIgnoreCase))
            {
                yatzisTotalText = candidate;
                Log("TextMeshProUGUI de Yatzis resuelto por nombre: " + candidate.gameObject.name);
                return;
            }
        }

        for (int i = 0; i < candidates.Length; i++)
        {
            TextMeshProUGUI candidate = candidates[i];
            if (candidate == null)
            {
                continue;
            }

            string candidateName = candidate.gameObject.name;
            if (candidateName.IndexOf("yatz", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                yatzisTotalText = candidate;
                Log("TextMeshProUGUI de Yatzis resuelto por coincidencia parcial de nombre: " + candidateName);
                return;
            }
        }
    }

    private bool TryResolveFromModule(EconomyModule module)
    {
        if (module == null)
        {
            return false;
        }

        IGameDataService moduleService = module.GameDataService;
        if (moduleService is GameDataService concreteService)
        {
            gameDataService = concreteService;
            return true;
        }

        return false;
    }

    private static string NormalizeSceneName(string rawSceneName)
    {
        if (string.IsNullOrWhiteSpace(rawSceneName))
        {
            return rawSceneName;
        }

        return rawSceneName.Trim().Trim('"');
    }
}