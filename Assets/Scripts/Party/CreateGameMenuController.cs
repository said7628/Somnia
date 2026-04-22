using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Somnia.Economy.Core;
using Somnia.Economy.Services;

namespace Somnia.UnityClient
{
    public class CreateGameMenuController : MonoBehaviour
    {
        [Header("Ranuras visuales")]
        [SerializeField] private SaveRanura[] ranuras;

        [Header("Escenas")]
        [SerializeField] private string nextSceneName = "Mapa";
        [SerializeField] private string escenaMenuPrincipal = "Pantalla_principal";

        [Header("Toast")]
        [SerializeField] private ToastMessage toastMessage;

        [Header("Nombres por defecto")]
        [SerializeField] private string slot1DefaultName = "Partida 1";
        [SerializeField] private string slot2DefaultName = "Partida 2";
        [SerializeField] private string slot3DefaultName = "Partida 3";

        [Header("Comportamiento")]
        [SerializeField] private bool permitirEntrarASlotOcupado = false;
        [SerializeField] private string loadingMessage = "Cargando...";

        private GameDataService gameDataService;

        private bool[] occupiedSlots = new bool[3];
        private bool isBusy;
        private bool slotsLoaded;

        private GameObject loadingBlocker;
        private CanvasGroup loadingCanvasGroup;
        private TextMeshProUGUI loadingText;

        private void Start()
        {
            Debug.Log("CreateGameMenuController: Start()");
            TryResolveToastMessage();
            InicializarRanurasVisualesVacias();
            ResolveGameDataService();
            StartCoroutine(LoadSlotsRoutine());
        }

        private void InicializarRanurasVisualesVacias()
        {
            if (ranuras == null || ranuras.Length == 0)
            {
                Debug.LogWarning("CreateGameMenuController: no hay ranuras asignadas.");
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
                Debug.Log($"CreateGameMenuController: listener asignado slot={slotNumber} interactable={ranuras[i].IsInteractable}");
            }
        }

        private void ResolveGameDataService()
        {
            if (gameDataService != null)
            {
                return;
            }

            var module = EconomyModule.Instance ?? FindFirstObjectByType<EconomyModule>();
            if (module == null)
            {
                var go = new GameObject("[EconomyModule]");
                module = go.AddComponent<EconomyModule>();
            }

            gameDataService = module.GameDataService as GameDataService;
        }

        public void OnClickCreateSlot(int slotNumber)
        {
            OnSlotClicked(slotNumber);
        }

        public void OnSlotClicked(int slotNumber)
        {
            Debug.Log($"CreateGameMenuController: click slot={slotNumber} isBusy={isBusy} slotsLoaded={slotsLoaded} occupied=[{FormatOccupiedSlots()}]");
            if (isBusy)
            {
                Debug.Log("CreateGameMenuController: click ignorado por estado busy.");
                return;
            }

            if (!slotsLoaded)
            {
                ShowToast("Todavia se estan cargando los slots.");
                return;
            }

            if (slotNumber < 1 || slotNumber > occupiedSlots.Length)
            {
                ShowToast("Slot invalido.");
                return;
            }

            if (!occupiedSlots[slotNumber - 1])
            {
                StartCoroutine(CreateSlotRoutine(slotNumber));
                return;
            }

            if (!permitirEntrarASlotOcupado)
            {
                ShowToast("Ese slot ya esta ocupado.");
                Debug.LogWarning($"CreateGameMenuController: slot {slotNumber} ocupado. Toast mostrado.");
                return;
            }

            if (GameSessionManager.Instance != null)
            {
                GameSessionManager.Instance.SetCurrentSlot(slotNumber);
            }

            SceneManager.LoadScene(nextSceneName);
        }

        public bool IsSlotOccupied(int slotNumber)
        {
            if (slotNumber < 1 || slotNumber > occupiedSlots.Length)
            {
                return false;
            }

            return occupiedSlots[slotNumber - 1];
        }

        private IEnumerator LoadSlotsRoutine()
        {
            Debug.Log($"CreateGameMenuController: LoadSlotsRoutine BEFORE reset occupied=[{FormatOccupiedSlots()}]");
            SetBusyState(true, "Cargando slots");
            slotsLoaded = false;

            if (gameDataService == null)
            {
                ShowToast("No se asigno GameDataService.");
                SetBusyState(false, "GameDataService null");
                yield break;
            }

            var task = gameDataService.GetSlotsAsync();
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.IsFaulted || task.Result == null)
            {
                ShowToast("No se pudieron cargar los slots.");
                SetBusyState(false, "Error cargando slots");
                yield break;
            }

            var response = task.Result;
            Debug.Log($"CreateGameMenuController: GetSlots backend success={response.success} message={response.message}");

            if (!response.success)
            {
                ShowToast(string.IsNullOrWhiteSpace(response.message)
                    ? "No se pudieron cargar los slots."
                    : response.message);
                SetBusyState(false, "Backend devolvio fallo de slots");
                yield break;
            }

            occupiedSlots = new bool[3];

            if (response.data != null)
            {
                for (int i = 0; i < response.data.Length; i++)
                {
                    var slot = response.data[i];
                    if (slot == null) continue;
                    if (slot.slot_numero < 1 || slot.slot_numero > occupiedSlots.Length) continue;

                    occupiedSlots[slot.slot_numero - 1] = slot.occupied;
                }
            }

            slotsLoaded = true;
            Debug.Log($"CreateGameMenuController: LoadSlotsRoutine AFTER load occupied=[{FormatOccupiedSlots()}]");
            RefreshSlotsVisuals();
            SetBusyState(false, "Slots cargados");
        }

        private IEnumerator CreateSlotRoutine(int slotNumber)
        {
            Debug.Log($"CreateGameMenuController: CreateSlotRoutine slot={slotNumber} BEFORE create occupied=[{FormatOccupiedSlots()}]");
            Debug.Log("[NewGame] Initial Yatzis = 0");
            SetBusyState(true, $"Creando slot {slotNumber}");

            if (gameDataService == null)
            {
                ShowToast("No se asigno GameDataService.");
                SetBusyState(false, "GameDataService null al crear");
                yield break;
            }

            string slotName = GetDefaultSlotName(slotNumber);

            var task = gameDataService.InitializeNewGameAsync(slotNumber, slotName);
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.IsFaulted || task.Result == null)
            {
                ShowToast("No se pudo crear la partida.");
                SetBusyState(false, $"Error creando slot {slotNumber}");
                yield break;
            }

            var response = task.Result;
            Debug.Log($"CreateGameMenuController: create/init response slot={slotNumber} success={response.success} message={response.message}");
            int backendYatzis = Mathf.Max(0, response.data?.slot != null ? response.data.slot.yatzis : 0);
            Debug.Log($"[NewGame] Backend Yatzis received = {backendYatzis}");
            Memoria_Islas.misMonedas = backendYatzis;

            if (!response.success)
            {
                var backendMessage = string.IsNullOrWhiteSpace(response.message)
                    ? "No se pudo crear la partida."
                    : response.message;

                ShowToast(backendMessage);

                Debug.LogWarning($"CreateGameMenuController: create failed slot={slotNumber}. Reloading from backend for authoritative state.");
                yield return LoadSlotsRoutine();
                SetBusyState(false, $"Create fallida slot {slotNumber}");
                yield break;
            }

            occupiedSlots[slotNumber - 1] = true;
            Debug.Log($"CreateGameMenuController: create success slot={slotNumber} occupied AFTER create=[{FormatOccupiedSlots()}]");
            RefreshSlotsVisuals();

            if (GameSessionManager.Instance != null)
            {
                GameSessionManager.Instance.SetCurrentSlot(slotNumber);
            }

            SetBusyState(false, $"Create exitosa slot {slotNumber}");
            SceneManager.LoadScene(nextSceneName);
        }

        private void RefreshSlotsVisuals()
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

                if (IsSlotOccupied(slotNumber))
                {
                    ranuras[i].ShowData(1);
                }
                else
                {
                    ranuras[i].ShowEmpty();
                }

                int capturedSlot = slotNumber;
                ranuras[i].ConfigureButton(() => OnSlotClicked(capturedSlot));
                Debug.Log($"CreateGameMenuController: Refresh slot={slotNumber} occupied={IsSlotOccupied(slotNumber)} interactable={ranuras[i].IsInteractable}");
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
            Debug.Log($"CreateGameMenuController: SetBusyState={busy}. reason={reason}");
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
                Debug.LogWarning("CreateGameMenuController: no se encontro Canvas para crear modal de carga.");
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
            Debug.Log("CreateGameMenuController: modal runtime 'Cargando...' creado por codigo.");
        }

        public void IrMenuPrincipal()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(escenaMenuPrincipal);
        }

        private void ShowToast(string message)
        {
            TryResolveToastMessage();
            Debug.Log($"CreateGameMenuController.Toast => {message}");
            if (toastMessage != null)
            {
                toastMessage.Show(message);
            }
            else
            {
                Debug.LogWarning(message);
            }
        }

        private void TryResolveToastMessage()
        {
            if (toastMessage != null)
            {
                return;
            }

            toastMessage = FindFirstObjectByType<ToastMessage>(FindObjectsInactive.Include);
            if (toastMessage != null)
            {
                Debug.Log("CreateGameMenuController: ToastMessage encontrado automaticamente.");
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

        private string FormatOccupiedSlots()
        {
            return string.Join(", ", occupiedSlots.Select((value, idx) => $"S{idx + 1}:{value}"));
        }
    }
}