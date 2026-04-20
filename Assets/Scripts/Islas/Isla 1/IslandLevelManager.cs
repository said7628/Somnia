using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Somnia.Economy.DTOs;
using Somnia.Economy.Services;
using Somnia.UnityClient;

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

        if (!TryResolveGameDataService())
        {
            Debug.LogError("IslandLevelManager: no se pudo resolver GameDataService desde el objeto asignado.");
            ApplyFallbackRules();
            return;
        }

        await LoadStateAsync();
    }

    private int ResolveCurrentSlot()
    {
        if (GameSessionManager.Instance != null)
        {
            return GameSessionManager.Instance.CurrentSlotNumber;
        }

        return 1;
    }

    private bool TryResolveGameDataService()
    {
        if (gameDataService != null)
        {
            return true;
        }

        if (economyModuleBehaviour == null)
        {
            Debug.LogError("IslandLevelManager: economyModuleBehaviour no esta asignado.");
            return false;
        }

        Type moduleType = economyModuleBehaviour.GetType();

        PropertyInfo property = moduleType.GetProperty(
            "GameDataService",
            BindingFlags.Public | BindingFlags.Instance
        );

        if (property == null)
        {
            Debug.LogError(
                "IslandLevelManager: el objeto asignado no tiene una propiedad publica llamada GameDataService."
            );
            return false;
        }

        object value = property.GetValue(economyModuleBehaviour);

        gameDataService = value as GameDataService;

        if (gameDataService == null)
        {
            Debug.LogError(
                "IslandLevelManager: la propiedad GameDataService existe, pero no se pudo castear a Somnia.Economy.Services.GameDataService."
            );
            return false;
        }

        return true;
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

            ProgresoDto[] progreso = response.data.progreso;

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
            return;
        }

        if (isLoading)
        {
            Log("Manager ocupado.");
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
            Log("Nivel bloqueado: " + dbLevelId);
            return;
        }

        if (string.IsNullOrWhiteSpace(level.sceneName))
        {
            Log("Nivel " + dbLevelId + " sin escena asignada. Se queda bloqueado.");
            return;
        }

        await MarkLevelAsPlayedAsync(dbLevelId);

        Log("Cargando escena: " + level.sceneName);
        SceneManager.LoadScene(level.sceneName);
    }

    private async Task MarkLevelAsPlayedAsync(int dbLevelId)
    {
        if (playedLevels.Contains(dbLevelId))
        {
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

            var saveResponse = await gameDataService.SaveProgressAsync(currentSlot, updatedProgress);

            if (saveResponse == null || !saveResponse.success)
            {
                Debug.LogWarning(
                    "IslandLevelManager: SaveProgressAsync fallo para nivel " +
                    dbLevelId + ". " +
                    (saveResponse != null ? saveResponse.message : "respuesta nula")
                );
            }

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
        if (levels.Length > 0 && levels[0] != null)
        {
            levels[0].isUnlocked = true;
        }

        // Nivel 2 solo si ya jugo el 1
        if (levels.Length > 1 && levels[1] != null && levels[0] != null)
        {
            levels[1].isUnlocked =
                playedLevels.Contains(levels[0].dbLevelId) ||
                completedLevels.Contains(levels[0].dbLevelId);
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
}