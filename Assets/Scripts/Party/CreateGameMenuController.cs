using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Somnia.Economy.Services;

namespace Somnia.UnityClient
{
    public class CreateGameMenuController : MonoBehaviour
    {
        [Header("Escena a cargar al crear partida")]
        [SerializeField] private string nextSceneName = "Menu";

        [Header("Toast")]
        [SerializeField] private ToastMessage toastMessage;

        [Header("Nombres por defecto")]
        [SerializeField] private string slot1DefaultName = "Partida 1";
        [SerializeField] private string slot2DefaultName = "Partida 2";
        [SerializeField] private string slot3DefaultName = "Partida 3";

        [Header("Referencia directa al servicio")]
        [SerializeField] private GameDataService gameDataService;

        private bool[] occupiedSlots = new bool[3];
        private bool isBusy;
        private bool slotsLoaded;

        private void Start()
        {
            StartCoroutine(LoadSlotsRoutine());
        }

        public void OnClickCreateSlot(int slotNumber)
        {
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

            if (occupiedSlots[slotNumber - 1])
            {
                ShowToast("Ese espacio ya esta ocupado.");
                return;
            }

            StartCoroutine(CreateSlotRoutine(slotNumber));
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

            if (!response.success)
            {
                ShowToast(string.IsNullOrWhiteSpace(response.message) ? "No se pudieron cargar los slots." : response.message);
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
        }

        private IEnumerator CreateSlotRoutine(int slotNumber)
        {
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

            if (!response.success)
            {
                var backendMessage = string.IsNullOrWhiteSpace(response.message)
                    ? "No se pudo crear la partida."
                    : response.message;

                ShowToast(backendMessage);

                if (backendMessage.ToLower().Contains("ocupado"))
                {
                    occupiedSlots[slotNumber - 1] = true;
                }

                yield break;
            }

            occupiedSlots[slotNumber - 1] = true;
            SceneManager.LoadScene(nextSceneName);
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
    }
}