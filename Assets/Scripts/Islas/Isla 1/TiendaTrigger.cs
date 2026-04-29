using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TiendaTrigger : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string tiendaSceneName = "Tienda";
    [SerializeField] private bool verboseLogs = true;

    private bool playerInside;
    private Transform playerTransform;
    private Collider2D ownCollider;

    private void Awake()
    {
        ownCollider = GetComponent<Collider2D>();
        if (verboseLogs)
        {
            Debug.Log($"[TiendaTrigger] Awake object={name} sceneName={tiendaSceneName} playerTag={playerTag} colliderIsTrigger={(ownCollider != null && ownCollider.isTrigger)}");
        }

        if (ownCollider == null)
        {
            Debug.LogWarning("[TiendaTrigger][WARN] Object has TiendaTrigger but no Collider2D");
        }
        else if (!ownCollider.isTrigger)
        {
            Debug.LogWarning("[TiendaTrigger][WARN] Collider2D exists but IsTrigger is false");
        }
    }

    private void Update()
    {
        bool ePressed = (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) || Input.GetKeyDown(KeyCode.E);
        if (!ePressed) return;

        if (!playerInside)
        {
            if (verboseLogs) Debug.LogWarning("[TiendaTrigger][WARN] E pressed but player is not inside trigger");
            return;
        }

        if (string.IsNullOrWhiteSpace(tiendaSceneName))
        {
            Debug.LogError("[TiendaTrigger][ERROR] tiendaSceneName is empty");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(tiendaSceneName))
        {
            Debug.LogError($"[TiendaTrigger][ERROR] Scene not found in build settings: {tiendaSceneName}");
            return;
        }

        string activeSceneName = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrWhiteSpace(activeSceneName) && activeSceneName.StartsWith("Isla"))
        {
            TextTypingSession.TrackReturnContext(activeSceneName, playerTransform != null ? playerTransform.position : Vector3.zero, playerTransform != null ? playerTransform.rotation : Quaternion.identity);
        }

        if (verboseLogs) Debug.Log($"[TiendaTrigger] E pressed inside trigger, loading scene={tiendaSceneName}");
        SceneManager.LoadScene(tiendaSceneName);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInside = true;
        playerTransform = other.transform;
        if (verboseLogs) Debug.Log($"[TiendaTrigger] Player entered trigger object={other.name}");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInside = false;
        playerTransform = null;
        if (verboseLogs) Debug.Log($"[TiendaTrigger] Player exited trigger object={other.name}");
    }
}