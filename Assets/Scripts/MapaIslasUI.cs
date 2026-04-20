using System.Collections;
using System.Threading.Tasks;
using Somnia.Economy.Core;
using Somnia.Economy.DTOs;
using Somnia.Economy.Services;
using Somnia.UnityClient;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MapaIslasUI : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private ToastUI toastUI;

    [SerializeField] private string sceneBosque = "Isla1";
    [SerializeField] private string sceneNieve = "Mapa2";
    [SerializeField] private string sceneCiudad = "Mapa3";
    [SerializeField] private bool verboseLogs = true;

    private Button bosqueButton;
    private Button nieveButton;
    private Button ciudadButton;

    private GameDataService gameDataService;
    private int highestUnlockedIsland = 1;

    private void Awake()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        if (uiDocument == null)
        {
            uiDocument = FindFirstObjectByType<UIDocument>();
        }

        if (toastUI == null)
        {
            toastUI = GetComponent<ToastUI>();
        }

        if (toastUI == null)
        {
            toastUI = FindFirstObjectByType<ToastUI>();
        }
    }

    private void OnEnable()
    {
        if (uiDocument == null)
        {
            Debug.LogError("[MapaIslasUI] No hay UIDocument asignado.");
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;

        bosqueButton = root.Q<Button>("bosque");
        nieveButton = root.Q<Button>("nieve");
        ciudadButton = root.Q<Button>("ciudad");

        EnsureDecorativeElementsDontBlock(root);
        EnsureIslandVisualPriority(bosqueButton, "bosque");
        EnsureIslandVisualPriority(nieveButton, "nieve");
        EnsureIslandVisualPriority(ciudadButton, "ciudad");

        StartCoroutine(LoadProgressRoutine());
    }

    private void EnsureIslandVisualPriority(Button button, string islandName)
    {
        if (button == null)
        {
            Debug.LogWarning("[MapaIslasUI] No se encontro boton para isla: " + islandName);
            return;
        }

        button.pickingMode = PickingMode.Position;
        button.BringToFront();

        foreach (VisualElement child in button.Children())
        {
            child.pickingMode = PickingMode.Ignore;
        }

        Log("Prioridad visual/hitbox revisada para " + islandName);
    }

    private void EnsureDecorativeElementsDontBlock(VisualElement root)
    {
        if (root == null)
        {
            return;
        }

        VisualElement caminoGrande = root.Q<VisualElement>("caminoGrande");
        VisualElement caminoChico = root.Q<VisualElement>("caminoChico");
        VisualElement agua = root.Q<VisualElement>("agua");
        VisualElement fondo = root.Q<VisualElement>("fondo");

        if (caminoGrande != null) caminoGrande.pickingMode = PickingMode.Ignore;
        if (caminoChico != null) caminoChico.pickingMode = PickingMode.Ignore;
        if (fondo != null) fondo.pickingMode = PickingMode.Ignore;

        if (agua != null)
        {
            agua.pickingMode = PickingMode.Ignore;

            foreach (VisualElement child in agua.Children())
            {
                if (child != bosqueButton && child != nieveButton && child != ciudadButton)
                {
                    child.pickingMode = PickingMode.Ignore;
                }
            }
        }
    }

    private IEnumerator LoadProgressRoutine()
    {
        yield return ResolveGameDataServiceRoutine();

        if (gameDataService == null)
        {
            ShowToast("No se pudo cargar progreso del mapa.");
            yield break;
        }

        int slot = 1;
        if (GameSessionManager.Instance != null)
        {
            slot = Mathf.Max(1, GameSessionManager.Instance.CurrentSlotNumber);
        }

        Task<ApiResponse<SlotDetailResponse>> task = gameDataService.GetSlotDetailAsync(slot);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted || task.Result == null || !task.Result.success || task.Result.data == null)
        {
            Log("No se pudo leer progreso. Se usa solo bosque desbloqueado.");
            highestUnlockedIsland = 1;
            yield break;
        }

        highestUnlockedIsland = ComputeHighestIsland(task.Result.data.progreso);
        Log("Progreso mapa cargado. highestUnlockedIsland=" + highestUnlockedIsland + " slot=" + slot);
    }

    private IEnumerator ResolveGameDataServiceRoutine()
    {
        EconomyModule module = EconomyModule.Instance ?? FindFirstObjectByType<EconomyModule>();

        if (module == null)
        {
            Log("No se encontro EconomyModule en mapa.");
            yield break;
        }

        float timeout = 5f;
        float elapsed = 0f;

        while (module.GameDataService == null && elapsed < timeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        gameDataService = module.GameDataService as GameDataService;

        if (gameDataService == null)
        {
            Debug.LogWarning("[MapaIslasUI] No se resolvio GameDataService.");
        }
    }

    public void OnBosqueClick()
    {
        Log("Click en Bosque -> " + sceneBosque);
        SceneManager.LoadScene(sceneBosque);
    }

    public void OnNieveClick()
    {
        Log("Click en Nieve.");

        if (highestUnlockedIsland >= 2)
        {
            SceneManager.LoadScene(sceneNieve);
            return;
        }

        ShowToast("Isla de nieve bloqueada.");
    }

    public void OnCiudadClick()
    {
        Log("Click en Ciudad.");

        if (highestUnlockedIsland >= 3)
        {
            SceneManager.LoadScene(sceneCiudad);
            return;
        }

        ShowToast("Ciudad bloqueada.");
    }

    private void ShowToast(string message)
    {
        Debug.Log("[MapaIslasUI] Toast => " + message);

        if (toastUI != null)
        {
            toastUI.ShowToast(message);
            return;
        }

        Debug.LogWarning("[MapaIslasUI] No hay referencia a ToastUI.");
    }

    private int ComputeHighestIsland(ProgresoDto[] progreso)
    {
        if (progreso == null || progreso.Length == 0)
        {
            return 1;
        }

        int best = 1;

        for (int i = 0; i < progreso.Length; i++)
        {
            if (progreso[i] == null)
            {
                continue;
            }

            best = Mathf.Max(best, progreso[i].id_isla);
        }

        return Mathf.Clamp(best, 1, 3);
    }

    private void Log(string message)
    {
        if (verboseLogs)
        {
            Debug.Log("[MapaIslasUI] " + message);
        }
    }
}