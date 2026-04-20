using UnityEngine;

public class LevelTrigger : MonoBehaviour
{
    [SerializeField] private int dbLevelId;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool verboseLogs = true;

    private bool playerInside;

    private void Update()
    {
        if (!playerInside) return;
        if (IslandLevelManager.Instance == null) return;
        if (!IslandLevelManager.Instance.IsInitialized) return;

        if (Input.GetKeyDown(interactKey))
        {
            if (verboseLogs)
            {
                Debug.Log("[LevelTrigger] Intentando entrar al nivel " + dbLevelId);
            }

            IslandLevelManager.Instance.TryEnterLevel(dbLevelId);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInside = true;

        if (verboseLogs)
        {
            Debug.Log("[LevelTrigger] Player entro al trigger del nivel " + dbLevelId);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInside = false;

        if (verboseLogs)
        {
            Debug.Log("[LevelTrigger] Player salio del trigger del nivel " + dbLevelId);
        }
    }
}