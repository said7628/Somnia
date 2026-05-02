using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MuelleTrigger : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string isla2SceneName = "Isla2";

    private bool playerInside;

    private void Awake()
    {
        Collider2D ownCollider = GetComponent<Collider2D>();
        Debug.Log($"[MuelleTrigger] Awake object={name} sceneName={isla2SceneName} playerTag={playerTag}");

        if (ownCollider == null || !ownCollider.isTrigger)
        {
            Debug.LogWarning("[MuelleTrigger][WARN] Collider2D missing or IsTrigger=false");
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.eKey.wasPressedThisFrame)
        {
            return;
        }

        if (!playerInside)
        {
            Debug.LogWarning("[MuelleTrigger][WARN] E pressed but player is not inside trigger");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(isla2SceneName))
        {
            Debug.LogError($"[MuelleTrigger][ERROR] Scene not found in Build Settings: {isla2SceneName}");
            return;
        }

        Debug.Log($"[MuelleTrigger] E pressed inside trigger, loading scene={isla2SceneName}");
        SceneManager.LoadScene(isla2SceneName);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        playerInside = true;
        Debug.Log($"[MuelleTrigger] Player entered trigger object={other.name}");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        playerInside = false;
        Debug.Log($"[MuelleTrigger] Player exited trigger object={other.name}");
    }
}