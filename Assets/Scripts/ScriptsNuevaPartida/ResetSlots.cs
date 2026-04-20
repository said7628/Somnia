using UnityEngine;

public class ResetSlots : MonoBehaviour
{
    [ContextMenu("Limpiar PlayerPrefs antiguos de slots")]
    public void ClearLegacySlotPrefs()
    {
        
        PlayerPrefs.DeleteKey("current_slot");
        PlayerPrefs.DeleteKey("slot_1");
        PlayerPrefs.DeleteKey("slot_2");
        PlayerPrefs.DeleteKey("slot_3");
        PlayerPrefs.Save();

        Debug.Log("ResetSlots: se limpiaron llaves legacy de PlayerPrefs (no usadas por flujo backend).");
    }
}