using UnityEngine;
using UnityEngine.UI;

namespace Somnia.UnityClient
{
    public class CreateGameSlotButton : MonoBehaviour
    {
        [SerializeField] private CreateGameMenuController menuController;
        [SerializeField] private Button button;
        [SerializeField] private int slotNumber = 1;

        private void Reset()
        {
            button = GetComponent<Button>();
        }

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (button != null)
            {
                button.onClick.RemoveListener(OnClickButton);
                button.onClick.AddListener(OnClickButton);
            }
        }

        private void OnClickButton()
        {
            if (menuController == null)
            {
                Debug.LogWarning("CreateGameSlotButton: falta asignar menuController.");
                return;
            }

            menuController.OnClickCreateSlot(slotNumber);
        }
    }
}