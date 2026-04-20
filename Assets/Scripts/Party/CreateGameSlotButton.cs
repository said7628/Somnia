using UnityEngine;
using UnityEngine.UI;

namespace Somnia.UnityClient
{
    public class CreateGameSlotButton : MonoBehaviour
    {
        [SerializeField] private CreateGameMenuController menuController;
        [SerializeField] private GuardadoBeta guardadoController;
        [SerializeField] private Button button;
        [SerializeField] private int slotNumber = 1;

        private void Reset()
        {
            button = GetComponent<Button>();
            if (guardadoController == null)
            {
                guardadoController = GetComponentInParent<GuardadoBeta>();
            }
        }

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (guardadoController == null)
            {
                guardadoController = GetComponentInParent<GuardadoBeta>();
            }

            if (button != null)
            {
                button.onClick.RemoveListener(OnClickButton);
                button.onClick.AddListener(OnClickButton);
            }
        }

        private void OnClickButton()
        {
            if (guardadoController != null)
            {
                guardadoController.OnSlotClicked(slotNumber);
                return;
            }

            if (menuController != null)
            {
                menuController.OnSlotClicked(slotNumber);
                return;
            }

            Debug.LogWarning("CreateGameSlotButton: falta asignar controlador de slots.");
        }
    }
}