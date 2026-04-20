using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
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

        private GameDataService gameDataService;

        [Header("Comportamiento")]
        [SerializeField] private bool permitirEntrarASlotOcupado = false;

        private bool[] occupiedSlots = new bool[3];
        private bool isBusy;
        private bool slotsLoaded;

        private void Start()
        {
            Debug.Log("CreateGameMenuController: Start()");
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
            isBusy = true;
            slotsLoaded = false;

            if (gameDataService == null)
            {
                ShowToast("No se asigno GameDataService.");
                isBusy = false;
                yield break;
            }

            var task = gameDataService.GetSlotsAsync();
            yield return new WaitUntil(() => task.IsCompleted);

            isBusy = false;

            if (task.IsFaulted || task.Result == null)
            {
                ShowToast("No se pudieron cargar los slots.");
                yield break;
            }

            var response = task.Result;
            Debug.Log($"CreateGameMenuController: GetSlots backend success={response.success} message={response.message}");

            if (!response.success)
            {
                ShowToast(string.IsNullOrWhiteSpace(response.message)
                    ? "No se pudieron cargar los slots."
                    : response.message);
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
        }

        private IEnumerator CreateSlotRoutine(int slotNumber)
        {
            Debug.Log($"CreateGameMenuController: CreateSlotRoutine slot={slotNumber} BEFORE create occupied=[{FormatOccupiedSlots()}]");
            isBusy = true;

            if (gameDataService == null)
            {
                ShowToast("No se asigno GameDataService.");
                isBusy = false;
                yield break;
            }

            string slotName = GetDefaultSlotName(slotNumber);

            var task = gameDataService.InitializeNewGameAsync(slotNumber, slotName);
            yield return new WaitUntil(() => task.IsCompleted);

            isBusy = false;

            if (task.IsFaulted || task.Result == null)
            {
                ShowToast("No se pudo crear la partida.");
                yield break;
            }

            var response = task.Result;
            Debug.Log($"CreateGameMenuController: create/init response slot={slotNumber} success={response.success} message={response.message}");

            if (!response.success)
            {
                var backendMessage = string.IsNullOrWhiteSpace(response.message)
                    ? "No se pudo crear la partida."
                    : response.message;

                ShowToast(backendMessage);

                Debug.LogWarning($"CreateGameMenuController: create failed slot={slotNumber}. Reloading from backend for authoritative state.");
                yield return LoadSlotsRoutine();

                yield break;
            }

            occupiedSlots[slotNumber - 1] = true;
            Debug.Log($"CreateGameMenuController: create success slot={slotNumber} occupied AFTER create=[{FormatOccupiedSlots()}]");
            RefreshSlotsVisuals();

            if (GameSessionManager.Instance != null)
            {
                GameSessionManager.Instance.SetCurrentSlot(slotNumber);
            }

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
                ranuras[i].SetInteractable(true);

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

        public void IrMenuPrincipal()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(escenaMenuPrincipal);
        }

        private void ShowToast(string message)
        {
            if (toastMessage != null)
            {
                toastMessage.Show(message);
            }
            else
            {
                Debug.LogWarning(message);
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