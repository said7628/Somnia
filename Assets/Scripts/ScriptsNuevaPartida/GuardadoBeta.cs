using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Somnia.Economy.Core;
using Somnia.Economy.Services;
using Somnia.UnityClient;

public class GuardadoBeta : MonoBehaviour
{
    [Header("Ranuras visuales")]
    [SerializeField] private SaveRanura[] ranuras;

    [Header("Toast opcional")]
    [SerializeField] private ToastMessage toastMessage;
    [SerializeField] private ToastUI toastUI;

    [Header("Escenas")]
    [SerializeField] private string escenaMapa = "Mapa";
    [SerializeField] private string escenaMenuPrincipal = "Pantalla_principal";
    [SerializeField] private string escenaIsla1 = "Isla1";
    [SerializeField] private string escenaIsla2 = "Isla2";
    [SerializeField] private string escenaIsla3 = "Isla3";

    [Header("Nombres por defecto")]
    [SerializeField] private string slot1DefaultName = "Partida 1";
    [SerializeField] private string slot2DefaultName = "Partida 2";
    [SerializeField] private string slot3DefaultName = "Partida 3";

    [Header("Debug")]
    [SerializeField] private bool verboseLogs;
    [Header("Loading modal runtime")]
    [SerializeField] private string loadingMessage = "Cargando...";
    [Header("Modo")]
    [SerializeField] private bool autoDetectModeBySceneName = true;
    [SerializeField] private bool continueMode;

    private GameDataService gameDataService;

    private bool[] occupiedSlots = new bool[3];
    private int[] highestUnlockedIslandBySlot = new int[3] { 1, 1, 1 };
    private bool isBusy;
    private bool slotsLoaded;

    private GameObject loadingBlocker;
    private CanvasGroup loadingCanvasGroup;
    private TextMeshProUGUI loadingText;
    private GameObject runtimeToastRoot;
    private TextMeshProUGUI runtimeToastText;
    private Coroutine runtimeToastCoroutine;

    private void Start()
    {
        AutoConfigureMode();
        Debug.Log("GuardadoBeta: Start()");
        TryResolveToastMessage();
        InicializarRanurasVisuales();
        BindTrashButtonsIfPresent();
        StartCoroutine(BootstrapAndLoadSlots());
    }

    private IEnumerator BootstrapAndLoadSlots()
    {
        yield return ResolveGameDataServiceRoutine();

        if (gameDataService == null)
        {
            MostrarToast("No se pudo inicializar el servicio de partidas.");
            Debug.LogError("GuardadoBeta: GameDataService sigue null despues del bootstrap.");
            yield break;
        }

        yield return CargarSlotsDesdeBackend();
    }

    private IEnumerator ResolveGameDataServiceRoutine()
    {
        if (gameDataService != null)
        {
            yield break;
        }

        var module = FindFirstObjectByType<EconomyModule>();

        if (module == null)
        {
            var moduleGo = new GameObject("[EconomyModule]");
            module = moduleGo.AddComponent<EconomyModule>();
            if (verboseLogs)
            {
                Debug.Log("GuardadoBeta: se creo EconomyModule runtime para resolver GameDataService.");
            }
        }

        const float timeoutSeconds = 5f;
        float elapsed = 0f;

        while (module != null && module.GameDataService == null && elapsed < timeoutSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        gameDataService = module?.GameDataService as GameDataService;

        if (gameDataService == null)
        {
            Debug.LogError("GuardadoBeta: EconomyModule existe pero su GameDataService no se pudo resolver.");
        }
    }

    private void InicializarRanurasVisuales()
    {
        if (ranuras == null || ranuras.Length == 0)
        {
            Debug.LogWarning("GuardadoBeta: no hay ranuras asignadas.");
            return;
        }

        for (int i = 0; i < ranuras.Length; i++)
        {
            if (ranuras[i] == null)
            {
                continue;
            }

            int slotNumber = i + 1;
            ranuras[i].SetSlotNumber(slotNumber);
            ranuras[i].ShowEmpty();
            ranuras[i].SetInteractable(true);

            int capturedSlot = slotNumber;
            ranuras[i].ConfigureButton(() => OnSlotClicked(capturedSlot));
            Debug.Log($"GuardadoBeta: listener asignado a slot {slotNumber}, interactable={ranuras[i].IsInteractable}");
        }
    }

    private IEnumerator CargarSlotsDesdeBackend()
    {
        Debug.Log($"GuardadoBeta: CargarSlotsDesdeBackend() BEFORE reset occupied=[{FormatOccupiedSlots()}]");
        SetBusyState(true, "Cargando slots");
        slotsLoaded = false;

        var task = gameDataService.GetSlotsAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted || task.Result == null)
        {
            MostrarToast("No se pudieron cargar los slots.");
            Debug.LogWarning($"GuardadoBeta: error cargando slots: {task.Exception}");
            SetBusyState(false, "Error cargando slots");
            yield break;
        }

        var response = task.Result;
        Debug.Log($"GuardadoBeta: GetSlots backend success={response.success} message={response.message}");

        if (!response.success)
        {
            MostrarToast(string.IsNullOrWhiteSpace(response.message)
                ? "No se pudieron cargar los slots."
                : response.message);
            SetBusyState(false, "Respuesta backend fallida al cargar slots");
            yield break;
        }

        occupiedSlots = new bool[3];
        highestUnlockedIslandBySlot = new int[3] { 1, 1, 1 };

        if (response.data != null)
        {
            for (int i = 0; i < response.data.Length; i++)
            {
                var slot = response.data[i];
                if (slot == null) continue;
                if (slot.slot_numero < 1 || slot.slot_numero > 3) continue;

                int index = slot.slot_numero - 1;
                occupiedSlots[index] = slot.occupied;
                Debug.Log($"GuardadoBeta: backend slot {slot.slot_numero} occupied={slot.occupied}");

                if (slot.occupied)
                {
                    yield return LoadSlotVisualData(slot.slot_numero);
                }
            }
        }

        slotsLoaded = true;
        Debug.Log($"GuardadoBeta: CargarSlotsDesdeBackend() AFTER load occupied=[{FormatOccupiedSlots()}]");
        RefreshSlots();
        SetBusyState(false, "Slots cargados");
    }

    private IEnumerator LoadSlotVisualData(int slotNumber)
    {
        var task = gameDataService.GetSlotDetailAsync(slotNumber);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted || task.Result == null || !task.Result.success || task.Result.data == null)
        {
            highestUnlockedIslandBySlot[slotNumber - 1] = 1;
            yield break;
        }

        ApplyBackendAge(task.Result.data.edad_jugador);
        highestUnlockedIslandBySlot[slotNumber - 1] = ComputeHighestIsland(task.Result.data.progreso);
    }

    public void OnSlotClicked(int slotNumber)
    {
        Debug.Log($"GuardadoBeta: click recibido slot={slotNumber} isBusy={isBusy} slotsLoaded={slotsLoaded} occupied=[{FormatOccupiedSlots()}]");
        if (isBusy)
        {
            Debug.Log("GuardadoBeta: click ignorado porque UI esta ocupada.");
            return;
        }

        if (!slotsLoaded)
        {
            MostrarToast("Todavia se estan cargando los slots.");
            return;
        }

        if (slotNumber < 1 || slotNumber > 3)
        {
            MostrarToast("Slot invalido.");
            return;
        }

        if (!occupiedSlots[slotNumber - 1])
        {
            if (continueMode)
            {
                MostrarToast("Ese slot esta vacio.");
                return;
            }

            StartCoroutine(CrearNuevaPartidaBackend(slotNumber));
            return;
        }

        if (continueMode)
        {
            StartCoroutine(ContinuarPartidaBackend(slotNumber));
            return;
        }

        MostrarToast("Ese slot ya esta ocupado.");
        ShowRuntimeToast("Ese slot ya esta ocupado.");
        Debug.Log($"[Toast] Slot ocupado mostrado slot={slotNumber}");
        Debug.LogWarning($"GuardadoBeta: slot {slotNumber} ocupado. Se mostro toast visual.");
    }

    private IEnumerator CrearNuevaPartidaBackend(int slotNumber)
    {
        Debug.Log($"GuardadoBeta: CrearNuevaPartidaBackend slot={slotNumber} BEFORE create occupied=[{FormatOccupiedSlots()}]");
        Debug.Log("[NewGame] Initial Yatzis = 0");
        SetBusyState(true, $"Creando slot {slotNumber}");

        string slotName = GetDefaultSlotName(slotNumber);

        var task = gameDataService.InitializeNewGameAsync(slotNumber, slotName);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted || task.Result == null)
        {
            MostrarToast("No se pudo crear la partida.");
            Debug.LogWarning($"GuardadoBeta: error creando partida slot {slotNumber}: {task.Exception}");
            SetBusyState(false, $"Error creando slot {slotNumber}");
            yield break;
        }

        var response = task.Result;
        Debug.Log($"GuardadoBeta: Create/init response slot={slotNumber} success={response.success} message={response.message}");
        int backendYatzis = Mathf.Max(0, response.data?.slot != null ? response.data.slot.yatzis : 0);
        Debug.Log($"[NewGame] Backend Yatzis received = {backendYatzis}");
        Memoria_Islas.misMonedas = backendYatzis;

        if (!response.success)
        {
            string backendMessage = string.IsNullOrWhiteSpace(response.message)
                ? "No se pudo crear la partida."
                : response.message;

            MostrarToast(backendMessage);
            Debug.LogWarning($"GuardadoBeta: create failed slot={slotNumber}, forcing authoritative reload from backend. occupied BEFORE reload=[{FormatOccupiedSlots()}]");
            yield return CargarSlotsDesdeBackend();
            Debug.LogWarning($"GuardadoBeta: create failed slot={slotNumber}, occupied AFTER reload=[{FormatOccupiedSlots()}]");
            SetBusyState(false, $"Create fallida slot {slotNumber}");
            yield break;
        }

        occupiedSlots[slotNumber - 1] = true;
        ApplyBackendAge(response.data?.edad_jugador ?? 0);
        highestUnlockedIslandBySlot[slotNumber - 1] = ComputeHighestIsland(response.data?.progreso);
        Debug.Log($"GuardadoBeta: create success slot={slotNumber} occupied AFTER create=[{FormatOccupiedSlots()}]");
        RefreshSlots();

        GameSessionManager.Instance?.SetCurrentSlot(slotNumber);
        SetBusyState(false, $"Create exitosa slot {slotNumber}");
        SceneManager.LoadScene(escenaMapa);
    }

    private IEnumerator ContinuarPartidaBackend(int slotNumber)
    {
        SetBusyState(true, $"Continuando slot {slotNumber}");
        GameSessionManager.Instance?.SetCurrentSlot(slotNumber);

        var task = gameDataService.GetSlotDetailAsync(slotNumber);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted || task.Result == null || !task.Result.success || task.Result.data == null)
        {
            MostrarToast("No se pudo leer el progreso de la partida.");
            SetBusyState(false, $"Error continuando slot {slotNumber}");
            yield break;
        }

        ApplyBackendAge(task.Result.data.edad_jugador);
        string targetScene = ResolveSceneForContinue(task.Result.data.progreso ?? task.Result.data.progress);
        SetBusyState(false, $"Continuacion lista slot {slotNumber}");
        SceneManager.LoadScene(targetScene);
    }

    public void OnDeleteSlot1() => StartCoroutine(DeleteSlotRoutine(1));
    public void OnDeleteSlot2() => StartCoroutine(DeleteSlotRoutine(2));
    public void OnDeleteSlot3() => StartCoroutine(DeleteSlotRoutine(3));

    private IEnumerator DeleteSlotRoutine(int slotNumber)
    {
        if (isBusy)
        {
            yield break;
        }

        if (!slotsLoaded)
        {
            MostrarToast("Todavia se estan cargando los slots.");
            yield break;
        }

        if (slotNumber < 1 || slotNumber > occupiedSlots.Length)
        {
            MostrarToast("Slot invalido.");
            yield break;
        }

        if (!occupiedSlots[slotNumber - 1])
        {
            Debug.Log($"[DeleteSlot] Deleting slot={slotNumber} skipped=empty");
            yield break;
        }

        Debug.Log($"[DeleteSlot] Deleting slot={slotNumber}");
        SetBusyState(true, $"Eliminando slot {slotNumber}");
        var task = gameDataService.DeleteSlotAsync(slotNumber);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted || task.Result == null)
        {
            Debug.LogWarning($"[DeleteSlot] Failed slot={slotNumber} error={task.Exception}");
            MostrarToast("No se pudo eliminar la partida.");
            SetBusyState(false, $"Error eliminando slot {slotNumber}");
            yield break;
        }

        var response = task.Result;
        if (!response.success)
        {
            string backendMessage = string.IsNullOrWhiteSpace(response.message)
                ? "No se pudo eliminar la partida."
                : response.message;
            MostrarToast(backendMessage);
            Debug.LogWarning($"[DeleteSlot] Failed slot={slotNumber} error={backendMessage}");
            Debug.LogWarning($"GuardadoBeta: delete slot failed slot={slotNumber} message={backendMessage}");
            SetBusyState(false, $"Delete fallida slot {slotNumber}");
            yield break;
        }

        occupiedSlots[slotNumber - 1] = false;
        highestUnlockedIslandBySlot[slotNumber - 1] = 1;
        Debug.Log($"[DeleteSlot] Success slot={slotNumber}");
        RefreshSlots();
        SetBusyState(false, $"Delete exitosa slot {slotNumber}");
        yield return CargarSlotsDesdeBackend();
    }

    public void RefreshSlots()
    {
        if (ranuras == null || ranuras.Length == 0)
        {
            return;
        }

        for (int i = 0; i < ranuras.Length; i++)
        {
            if (ranuras[i] == null)
            {
                continue;
            }

            int slotNumber = i + 1;
            ranuras[i].SetSlotNumber(slotNumber);
            ranuras[i].SetInteractable(!isBusy);

            if (occupiedSlots[slotNumber - 1])
            {
                ranuras[i].ShowData(highestUnlockedIslandBySlot[slotNumber - 1]);
            }
            else
            {
                ranuras[i].ShowEmpty();
            }

            int capturedSlot = slotNumber;
            ranuras[i].ConfigureButton(() => OnSlotClicked(capturedSlot));
            Debug.Log($"GuardadoBeta: RefreshSlots slot={slotNumber} occupied={occupiedSlots[slotNumber - 1]} interactable={ranuras[i].IsInteractable}");
        }
    }

    private void SetSlotsInteractable(bool interactable)
    {
        if (ranuras == null)
        {
            return;
        }

        for (int i = 0; i < ranuras.Length; i++)
        {
            if (ranuras[i] != null)
            {
                ranuras[i].SetInteractable(interactable);
            }
        }
    }

    private void SetBusyState(bool busy, string reason)
    {
        isBusy = busy;
        SetSlotsInteractable(!busy);
        SetLoadingModalVisible(busy);
        Debug.Log($"GuardadoBeta: SetBusyState={busy}. reason={reason}");
    }

    private void SetLoadingModalVisible(bool visible)
    {
        EnsureLoadingModal();
        if (loadingBlocker == null)
        {
            return;
        }

        if (loadingText != null)
        {
            loadingText.text = loadingMessage;
        }

        loadingBlocker.SetActive(visible);
        if (loadingCanvasGroup != null)
        {
            loadingCanvasGroup.alpha = visible ? 1f : 0f;
            loadingCanvasGroup.blocksRaycasts = visible;
            loadingCanvasGroup.interactable = visible;
        }
    }

    private void EnsureLoadingModal()
    {
        if (loadingBlocker != null)
        {
            return;
        }

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            parentCanvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        }

        if (parentCanvas == null)
        {
            Debug.LogWarning("GuardadoBeta: no se encontro Canvas para crear modal de carga.");
            return;
        }

        loadingBlocker = new GameObject("RuntimeLoadingBlocker", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        loadingBlocker.transform.SetParent(parentCanvas.transform, false);

        RectTransform blockerRect = loadingBlocker.GetComponent<RectTransform>();
        blockerRect.anchorMin = Vector2.zero;
        blockerRect.anchorMax = Vector2.one;
        blockerRect.offsetMin = Vector2.zero;
        blockerRect.offsetMax = Vector2.zero;

        Image blockerImage = loadingBlocker.GetComponent<Image>();
        blockerImage.color = new Color(0f, 0f, 0f, 0.45f);

        loadingCanvasGroup = loadingBlocker.GetComponent<CanvasGroup>();
        loadingCanvasGroup.alpha = 0f;
        loadingCanvasGroup.blocksRaycasts = true;
        loadingCanvasGroup.interactable = true;

        GameObject panel = new GameObject("RuntimeLoadingPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(loadingBlocker.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(420f, 140f);

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.08f, 0.1f, 0.17f, 0.95f);

        GameObject labelGo = new GameObject("RuntimeLoadingLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(panel.transform, false);
        RectTransform labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(20f, 20f);
        labelRect.offsetMax = new Vector2(-20f, -20f);

        loadingText = labelGo.GetComponent<TextMeshProUGUI>();
        loadingText.alignment = TextAlignmentOptions.Center;
        loadingText.fontSize = 44f;
        loadingText.fontStyle = FontStyles.Bold;
        loadingText.color = Color.white;
        loadingText.text = loadingMessage;

        loadingBlocker.transform.SetAsLastSibling();
        loadingBlocker.SetActive(false);
        Debug.Log("GuardadoBeta: modal runtime 'Cargando...' creado por codigo.");
    }

    public void IrMenuPrincipal()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(escenaMenuPrincipal);
    }

    private void MostrarToast(string mensaje)
    {
        TryResolveToastMessage();

        Debug.Log($"GuardadoBeta.Toast => {mensaje}");
        if (toastMessage != null)
        {
            toastMessage.Show(mensaje);
        }
        else if (toastUI != null)
        {
            toastUI.ShowToast(mensaje);
        }
        else
        {
            ShowRuntimeToast(mensaje);
        }
    }

    private void TryResolveToastMessage()
    {
        if (toastMessage == null)
        {
            toastMessage = FindFirstObjectByType<ToastMessage>(FindObjectsInactive.Include);
            if (toastMessage != null)
            {
                Debug.Log("GuardadoBeta: ToastMessage encontrado automaticamente en escena.");
            }
        }

        if (toastUI == null)
        {
            toastUI = FindFirstObjectByType<ToastUI>(FindObjectsInactive.Include);
            if (toastUI != null)
            {
                Debug.Log("GuardadoBeta: ToastUI encontrado automaticamente en escena.");
            }
        }
    }

    private void ShowRuntimeToast(string mensaje)
    {
        EnsureRuntimeToastCreated();
        if (runtimeToastRoot == null || runtimeToastText == null)
        {
            Debug.LogWarning(mensaje);
            return;
        }

        runtimeToastText.text = mensaje;
        runtimeToastRoot.SetActive(true);
        runtimeToastRoot.transform.SetAsLastSibling();

        if (runtimeToastCoroutine != null)
        {
            StopCoroutine(runtimeToastCoroutine);
        }

        runtimeToastCoroutine = StartCoroutine(HideRuntimeToastRoutine(2.8f));
    }

    private IEnumerator HideRuntimeToastRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (runtimeToastRoot != null)
        {
            runtimeToastRoot.SetActive(false);
        }

        runtimeToastCoroutine = null;
    }

    private void EnsureRuntimeToastCreated()
    {
        if (runtimeToastRoot != null && runtimeToastText != null)
        {
            return;
        }

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            parentCanvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        }

        if (parentCanvas == null)
        {
            return;
        }

        runtimeToastRoot = new GameObject("RuntimeOccupiedSlotToast", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        runtimeToastRoot.transform.SetParent(parentCanvas.transform, false);

        RectTransform toastRect = runtimeToastRoot.GetComponent<RectTransform>();
        toastRect.anchorMin = new Vector2(0.5f, 0f);
        toastRect.anchorMax = new Vector2(0.5f, 0f);
        toastRect.pivot = new Vector2(0.5f, 0f);
        toastRect.anchoredPosition = new Vector2(0f, 65f);
        toastRect.sizeDelta = new Vector2(980f, 160f);

        Image toastImage = runtimeToastRoot.GetComponent<Image>();
        toastImage.color = new Color(0f, 0f, 0f, 0.9f);

        CanvasGroup toastCanvas = runtimeToastRoot.GetComponent<CanvasGroup>();
        toastCanvas.alpha = 1f;
        toastCanvas.blocksRaycasts = false;
        toastCanvas.interactable = false;

        GameObject textGo = new GameObject("RuntimeOccupiedSlotToastText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(runtimeToastRoot.transform, false);

        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(28f, 20f);
        textRect.offsetMax = new Vector2(-28f, -20f);

        runtimeToastText = textGo.GetComponent<TextMeshProUGUI>();
        runtimeToastText.alignment = TextAlignmentOptions.Center;
        runtimeToastText.fontSize = 46f;
        runtimeToastText.fontStyle = FontStyles.Bold;
        runtimeToastText.color = Color.white;
        runtimeToastText.enableAutoSizing = true;
        runtimeToastText.fontSizeMin = 30f;
        runtimeToastText.fontSizeMax = 54f;
        runtimeToastText.text = string.Empty;

        runtimeToastRoot.SetActive(false);
        runtimeToastRoot.transform.SetAsLastSibling();
        Debug.Log("GuardadoBeta: toast runtime creado por codigo para feedback visual.");
    }

    private string GetDefaultSlotName(int slotNumber)
    {
        switch (slotNumber)
        {
            case 1: return slot1DefaultName;
            case 2: return slot2DefaultName;
            case 3: return slot3DefaultName;
            default: return "Partida " + slotNumber;
        }
    }

    private static int ComputeHighestIsland(ProgresoDto[] progreso)
    {
        if (progreso == null || progreso.Length == 0)
        {
            return 1;
        }

        int best = 1;

        for (int i = 0; i < progreso.Length; i++)
        {
            var p = progreso[i];
            if (p == null)
            {
                continue;
            }

            if (p.id_isla > best)
            {
                best = p.id_isla;
            }
        }

        return Mathf.Clamp(best, 1, 3);
    }

    private void AutoConfigureMode()
    {
        if (!autoDetectModeBySceneName)
        {
            return;
        }

        string sceneName = SceneManager.GetActiveScene().name;
        if (string.Equals(sceneName, "ContinuarPartida", StringComparison.OrdinalIgnoreCase))
        {
            continueMode = true;
        }
        else if (string.Equals(sceneName, "Nuevo juego", StringComparison.OrdinalIgnoreCase))
        {
            continueMode = false;
        }
    }

    private void BindTrashButtonsIfPresent()
    {
        BindTrashButton("Basura1", OnDeleteSlot1);
        BindTrashButton("Basura2", OnDeleteSlot2);
        BindTrashButton("Basura3", OnDeleteSlot3);
    }

    private static void BindTrashButton(string objectName, Action action)
    {
        GameObject buttonObject = GameObject.Find(objectName);
        if (buttonObject == null)
        {
            return;
        }

        Button button = buttonObject.GetComponent<Button>();
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => action?.Invoke());
    }

    private string ResolveSceneForContinue(ProgresoDto[] progreso)
    {
        int currentSlot = Mathf.Max(1, GameSessionManager.Instance != null ? GameSessionManager.Instance.CurrentSlotNumber : 1);
        string persistedIslandScene = GameSessionManager.Instance != null
            ? GameSessionManager.Instance.GetCurrentIslandSceneForSlot(currentSlot)
            : null;
        if (!string.IsNullOrWhiteSpace(persistedIslandScene))
        {
            return persistedIslandScene;
        }

        int island = 1;

        ProgresoDto latest = null;
        DateTime latestAttempt = DateTime.MinValue;

        if (progreso != null)
        {
            for (int i = 0; i < progreso.Length; i++)
            {
                var p = progreso[i];
                if (p == null || p.id_isla < 1 || p.id_isla > 3)
                {
                    continue;
                }

                if (!TryParseBackendDate(p.ultimo_intento, out DateTime parsed))
                {
                    continue;
                }

                if (latest == null || parsed > latestAttempt)
                {
                    latest = p;
                    latestAttempt = parsed;
                }
            }
        }

        if (latest != null)
        {
            island = Mathf.Clamp(latest.id_isla, 1, 3);
        }
        else
        {
            island = ComputeHighestIsland(progreso);
        }

        switch (island)
        {
            case 2: return escenaIsla2;
            case 3: return escenaIsla3;
            default: return escenaIsla1;
        }
    }

    private static bool TryParseBackendDate(string raw, out DateTime date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        return DateTime.TryParse(
                raw,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out date)
            || DateTime.TryParse(raw, out date);
    }

    private string FormatOccupiedSlots()
    {
        return string.Join(", ", occupiedSlots.Select((value, idx) => $"S{idx + 1}:{value}"));
    }

    private void ApplyBackendAge(int backendAge)
    {
        if (backendAge <= 0)
        {
            return;
        }

        GameSessionManager.Instance?.SetPlayerAgeFromBackend(backendAge);
        TextTypingSession.PlayerAge = backendAge;
        Debug.Log($"[Age] edad_jugador sincronizada={backendAge}");
    }
}