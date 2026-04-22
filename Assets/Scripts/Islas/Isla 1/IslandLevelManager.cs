using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Somnia.Economy.Core;
using Somnia.Economy.DTOs;
using Somnia.Economy.Services;
using Somnia.UnityClient;
using UnityEngine.UIElements;
using System.Linq;

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

    [Header("Arrastra aqui el objeto que tenga la propiedad GameDataService")]
    [SerializeField] private MonoBehaviour economyModuleBehaviour;

    [Header("Niveles configurados en orden")]
    [SerializeField] private LevelEntry[] levels;

    [Header("Config")]
    [SerializeField] private bool lockLevelsWithoutScene = true;
    [SerializeField] private bool verboseLogs = true;
    [SerializeField] private ToastMessage toastMessage;
    [SerializeField] private ToastUI toastUI;

    private GameDataService gameDataService;
    private int currentSlot = 1;
    private bool isLoading;
    private bool isInitialized;

    private readonly HashSet<int> playedLevels = new HashSet<int>();
    private readonly HashSet<int> completedLevels = new HashSet<int>();

    public bool IsInitialized
    {
        get { return isInitialized; }
    }

    public bool IsBusy
    {
        get { return isLoading; }
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
        Log("Current slot resolved for island flow=" + currentSlot);
        Log("Active scene on island start=" + SceneManager.GetActiveScene().name);
        TryResolveToast();

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

        if (TryResolveFromBehaviour(economyModuleBehaviour))
        {
            Log("GameDataService resuelto desde economyModuleBehaviour asignado.");
            return true;
        }

        if (economyModuleBehaviour == null)
        {
            economyModuleBehaviour = EconomyModule.Instance ?? FindFirstObjectByType<EconomyModule>(FindObjectsInactive.Include);
            if (economyModuleBehaviour != null)
            {
                Log("economyModuleBehaviour autodescubierto en escena.");
            }
        }

        if (TryResolveFromBehaviour(economyModuleBehaviour))
        {
            Log("GameDataService resuelto por autodescubrimiento.");
            return true;
        }

        EconomyModule runtimeModule = EconomyModule.Instance ?? FindFirstObjectByType<EconomyModule>(FindObjectsInactive.Include);
        if (runtimeModule == null)
        {
            runtimeModule = new GameObject("[EconomyModule]").AddComponent<EconomyModule>();
            Log("Se creo EconomyModule runtime para recuperar GameDataService.");
        }

        const float timeoutSeconds = 6f;
        float elapsed = 0f;
        while (runtimeModule != null && runtimeModule.GameDataService == null && elapsed < timeoutSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            await Task.Yield();
        }

        economyModuleBehaviour = runtimeModule;
        if (TryResolveFromBehaviour(economyModuleBehaviour))
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
        else if (toastUI != null)
        {
            toastUI.ShowToast(message);
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

        if (toastUI == null)
        {
            toastUI = FindFirstObjectByType<ToastUI>(FindObjectsInactive.Include);
            if (toastUI != null)
            {
                Log("ToastUI encontrado automaticamente.");
            }
        }
    }

    private bool TryResolveFromBehaviour(MonoBehaviour behaviour)
    {
        if (behaviour == null)
        {
            return false;
        }

        Type moduleType = behaviour.GetType();
        PropertyInfo property = moduleType.GetProperty("GameDataService", BindingFlags.Public | BindingFlags.Instance);
        if (property == null)
        {
            return false;
        }

        gameDataService = property.GetValue(behaviour) as GameDataService;
        return gameDataService != null;
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