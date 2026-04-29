using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TiendaTrigger : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string tiendaSceneName = "Tienda";
    [SerializeField] private bool verboseLogs = true;

    private bool playerInside;

    private void Update()
    {
        if (!playerInside || Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (verboseLogs)
            {
                Debug.Log("[TiendaTrigger] Loading scene Tienda");
            }

            SceneManager.LoadScene(tiendaSceneName);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInside = true;
        if (verboseLogs) Debug.Log("[TiendaTrigger] Player entered shop trigger");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInside = false;
        if (verboseLogs) Debug.Log("[TiendaTrigger] Player exited shop trigger");
    }
}
