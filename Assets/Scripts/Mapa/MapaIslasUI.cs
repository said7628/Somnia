using UnityEngine;
using UnityEngine.SceneManagement;

public class MapaIslasUI : MonoBehaviour
{
    [SerializeField] private ToastUI toastUI;
    [SerializeField] private string escenaIsla1 = "Isla1";

    private void Awake()
    {
        if (toastUI == null)
        {
            toastUI = GetComponent<ToastUI>();
        }

        if (toastUI == null)
        {
            toastUI = FindFirstObjectByType<ToastUI>();
        }
    }

    public void AbrirIsla1()
    {
        SceneManager.LoadScene(escenaIsla1);
    }

    public void MostrarIslaBloqueada()
    {
        Debug.Log("Click en isla bloqueada");

        if (toastUI != null)
        {
            toastUI.ShowToast("Esta isla aun no esta desbloqueada");
        }
        else
        {
            Debug.LogError("MapaIslasUI: no hay referencia a ToastUI");
        }
    }
}