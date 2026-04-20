using UnityEngine;
using UnityEngine.InputSystem;

public class LevelTrigger : MonoBehaviour
{
    [SerializeField] private int dbLevelId;
    [SerializeField] private Key interactKey = Key.E;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool acceptChildrenTagMatch = true;
    [SerializeField] private bool verboseLogs = true;
    [SerializeField] private bool showHintLog = true;

    private bool playerInside;
    private bool waitingForManagerLogShown;

    private void Update()
    {
        if (!playerInside)
        {
            return;
        }

        if (IslandLevelManager.Instance == null)
        {
            LogOnce("Esperando IslandLevelManager en escena...");
            return;
        }

        if (!IslandLevelManager.Instance.IsInitialized)
        {
            LogOnce("IslandLevelManager aun inicializando progreso...");
            return;
        }

        waitingForManagerLogShown = false;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (WasInteractPressed(keyboard))
        {
            if (verboseLogs)
            {
                Debug.Log("[LevelTrigger] Intentando entrar al nivel " + dbLevelId + " con tecla " + interactKey);
            }

            IslandLevelManager.Instance.TryEnterLevel(dbLevelId);
        }
        else if (showHintLog && keyboard.tabKey.wasPressedThisFrame)
        {
            Debug.Log("[LevelTrigger] Hint: dentro del trigger del nivel " + dbLevelId + ", presiona " + interactKey + " para entrar.");
        }
    }

    private bool WasInteractPressed(Keyboard keyboard)
    {
        switch (interactKey)
        {
            case Key.E:
                return keyboard.eKey.wasPressedThisFrame;
            case Key.F:
                return keyboard.fKey.wasPressedThisFrame;
            case Key.Space:
                return keyboard.spaceKey.wasPressedThisFrame;
            case Key.Enter:
                return keyboard.enterKey.wasPressedThisFrame;
            default:
                return false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TrySetPlayerInside(other.gameObject, true, "3D");
    }

    private void OnTriggerExit(Collider other)
    {
        TrySetPlayerInside(other.gameObject, false, "3D");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TrySetPlayerInside(other.gameObject, true, "2D");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        TrySetPlayerInside(other.gameObject, false, "2D");
    }

    private void TrySetPlayerInside(GameObject other, bool inside, string colliderType)
    {
        if (!IsPlayerObject(other))
        {
            return;
        }

        playerInside = inside;

        if (verboseLogs)
        {
            string action = inside ? "entro" : "salio";
            Debug.Log("[LevelTrigger] Player " + action + " al trigger nivel=" + dbLevelId + " (" + colliderType + ") objeto=" + other.name);
        }

        if (!inside)
        {
            waitingForManagerLogShown = false;
        }
    }

    private bool IsPlayerObject(GameObject other)
    {
        if (other == null)
        {
            return false;
        }

        if (other.CompareTag(playerTag))
        {
            return true;
        }

        if (!acceptChildrenTagMatch)
        {
            return false;
        }

        Transform t = other.transform;
        return (t.root != null && t.root.CompareTag(playerTag)) ||
               (t.parent != null && t.parent.CompareTag(playerTag)) ||
               other.GetComponentInParent<PlayerMovement>() != null ||
               other.GetComponentInParent<MoverConImputAction>() != null;
    }

    private void LogOnce(string message)
    {
        if (waitingForManagerLogShown)
        {
            return;
        }

        waitingForManagerLogShown = true;

        if (verboseLogs)
        {
            Debug.Log("[LevelTrigger] " + message);
        }
    }
}