using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Somnia.Economy.Core;
using Somnia.Economy.Services;
using Somnia.UnityClient;

public class GuardadoBeta : MonoBehaviour
{
    [Header("Ranuras visuales")]
    [SerializeField] private SaveRanura[] ranuras;

    [Header("Toast opcional")]
    [SerializeField] private ToastMessage toastMessage;

    [Header("Escenas")]
    [SerializeField] private string escenaMapa = "Mapa";
    [SerializeField] private string escenaMenuPrincipal = "Pantalla_principal";

    [Header("Nombres por defecto")]
    [SerializeField] private string slot1DefaultName = "Partida 1";
    [SerializeField] private string slot2DefaultName = "Partida 2";
    [SerializeField] private string slot3DefaultName = "Partida 3";

    [Header("Debug")]
    [SerializeField] private bool verboseLogs;

    private GameDataService gameDataService;

    private bool[] occupiedSlots = new bool[3];
    private int[] highestUnlockedIslandBySlot = new int[3] { 1, 1, 1 };
    private bool isBusy;
    private bool slotsLoaded;

    private void Start()
    {
        Debug.Log("GuardadoBeta: Start()");
        InicializarRanurasVisuales();
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

        // 1) Flujo oficial: EconomyModule ya presente en escena.
        var module = FindFirstObjectByType<EconomyModule>();

        // 2) Si no existe, lo creamos para no depender de referencias manuales rotas en el inspector.
        if (module == null)
        {
            var moduleGo = new GameObject("[EconomyModule]");
            module = moduleGo.AddComponent<EconomyModule>();
            if (verboseLogs)
            {
                Debug.Log("GuardadoBeta: se creo EconomyModule runtime para resolver GameDataService.");
            }
        }

        // 3) Esperamos un poco a que Awake() inicialice GameDataService.
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
        isBusy = true;
        slotsLoaded = false;

        var task = gameDataService.GetSlotsAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        isBusy = false;

        if (task.IsFaulted || task.Result == null)
        {
            MostrarToast("No se pudieron cargar los slots.");
            Debug.LogWarning($"GuardadoBeta: error cargando slots: {task.Exception}");
            yield break;
        }

        var response = task.Result;
        Debug.Log($"GuardadoBeta: GetSlots backend success={response.success} message={response.message}");

        if (!response.success)
        {
            MostrarToast(string.IsNullOrWhiteSpace(response.message)
                ? "No se pudieron cargar los slots."
                : response.message);
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

        highestUnlockedIslandBySlot[slotNumber - 1] = ComputeHighestIsland(task.Result.data.progreso);
    }

    public void OnSlotClicked(int slotNumber)
    {
        Debug.Log($"GuardadoBeta: click recibido slot={slotNumber} isBusy={isBusy} slotsLoaded={slotsLoaded} occupied=[{FormatOccupiedSlots()}]");
        if (isBusy)
        {
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
            StartCoroutine(CrearNuevaPartidaBackend(slotNumber));
            return;
        }

        MostrarToast("Ese slot ya esta ocupado.");
    }

    private IEnumerator CrearNuevaPartidaBackend(int slotNumber)
    {
        Debug.Log($"GuardadoBeta: CrearNuevaPartidaBackend slot={slotNumber} BEFORE create occupied=[{FormatOccupiedSlots()}]");
        isBusy = true;

        string slotName = GetDefaultSlotName(slotNumber);

        var task = gameDataService.InitializeNewGameAsync(slotNumber, slotName);
        yield return new WaitUntil(() => task.IsCompleted);

        isBusy = false;

        if (task.IsFaulted || task.Result == null)
        {
            MostrarToast("No se pudo crear la partida.");
            Debug.LogWarning($"GuardadoBeta: error creando partida slot {slotNumber}: {task.Exception}");
            yield break;
        }

        var response = task.Result;
        Debug.Log($"GuardadoBeta: Create/init response slot={slotNumber} success={response.success} message={response.message}");

        if (!response.success)
        {
            string backendMessage = string.IsNullOrWhiteSpace(response.message)
                ? "No se pudo crear la partida."
                : response.message;

            MostrarToast(backendMessage);
            Debug.LogWarning($"GuardadoBeta: create failed slot={slotNumber}, forcing authoritative reload from backend. occupied BEFORE reload=[{FormatOccupiedSlots()}]");
            yield return CargarSlotsDesdeBackend();
            Debug.LogWarning($"GuardadoBeta: create failed slot={slotNumber}, occupied AFTER reload=[{FormatOccupiedSlots()}]");

            yield break;
        }

        occupiedSlots[slotNumber - 1] = true;
        highestUnlockedIslandBySlot[slotNumber - 1] = ComputeHighestIsland(response.data?.progreso);
        Debug.Log($"GuardadoBeta: create success slot={slotNumber} occupied AFTER create=[{FormatOccupiedSlots()}]");
        RefreshSlots();

        GameSessionManager.Instance?.SetCurrentSlot(slotNumber);
        SceneManager.LoadScene(escenaMapa);
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
            ranuras[i].SetInteractable(true);

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

    public void IrMenuPrincipal()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(escenaMenuPrincipal);
    }

    private void MostrarToast(string mensaje)
    {
        if (toastMessage != null)
        {
            toastMessage.Show(mensaje);
        }
        else
        {
            Debug.LogWarning(mensaje);
        }
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

            // Si no viene id_isla, mantenemos convención visual mínima en isla 1.
            if (p.id_isla > best)
            {
                best = p.id_isla;
            }
        }

        return Mathf.Clamp(best, 1, 3);
    }

    private string FormatOccupiedSlots()
    {
        return string.Join(", ", occupiedSlots.Select((value, idx) => $"S{idx + 1}:{value}"));
    }
}