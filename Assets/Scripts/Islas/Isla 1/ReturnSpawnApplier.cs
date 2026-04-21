using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnSpawnApplier : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool verboseLogs = true;
    [SerializeField] private int maxFramesToFindPlayer = 120;
    [SerializeField] private int verifyFramesAfterApply = 10;
    [SerializeField] private float overwriteDistanceTolerance = 0.15f;
    [SerializeField] private bool retryOnceIfOverwritten = true;

    private IEnumerator Start()
    {
        yield return null;

        string activeScene = SceneManager.GetActiveScene().name;
        string targetScene = TextTypingSession.ResolveReturnScene();

        if (!TextTypingSession.HasReturnTransform)
        {
            Log($"No return transform saved. Keeping default spawn in scene={activeScene}");
            yield break;
        }

        if (!string.Equals(activeScene, targetScene, System.StringComparison.Ordinal))
        {
            Log($"Return context scene mismatch. activeScene={activeScene} expected={targetScene}");
            yield break;
        }

        Log($"Return scene={targetScene}");
        Log($"Saved return position={TextTypingSession.ReturnPosition}");
        Log($"Saved return rotation={TextTypingSession.ReturnRotation.eulerAngles}");

        GameObject player = null;
        for (int frame = 0; frame < maxFramesToFindPlayer; frame++)
        {
            player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
            {
                break;
            }

            yield return null;
        }

        if (player == null)
        {
            Debug.LogWarning($"[ReturnSpawnApplier] Player with tag='{playerTag}' not found. Could not apply return spawn.");
            yield break;
        }

        Log($"Player found name={player.name} positionBeforeApply={player.transform.position}");

        ApplyReturnTransform(player.transform);

        bool overwritten = false;

        for (int frame = 0; frame < verifyFramesAfterApply; frame++)
        {
            yield return null;

            float distance = Vector3.Distance(player.transform.position, TextTypingSession.ReturnPosition);
            if (distance > overwriteDistanceTolerance)
            {
                overwritten = true;
                break;
            }
        }

        Log($"Return spawn applied successfully={!overwritten}");
        Log($"Was return spawn overwritten after apply={overwritten}");

        if (overwritten && retryOnceIfOverwritten)
        {
            Log("Detected overwrite. Reapplying return spawn once.");
            ApplyReturnTransform(player.transform);

            yield return null;

            float finalDistance = Vector3.Distance(player.transform.position, TextTypingSession.ReturnPosition);
            bool stillOverwritten = finalDistance > overwriteDistanceTolerance;
            Log($"Return spawn final verification success={!stillOverwritten} currentPosition={player.transform.position}");
        }
    }

    private void ApplyReturnTransform(Transform playerTransform)
    {
        playerTransform.SetPositionAndRotation(TextTypingSession.ReturnPosition, TextTypingSession.ReturnRotation);

        Log(
            $"Applied return spawn scene={SceneManager.GetActiveScene().name} sourceIslandScene={TextTypingSession.SourceIslandSceneName} " +
            $"sourceLevelScene={TextTypingSession.LastLevelSceneName} returnPosition={TextTypingSession.ReturnPosition} " +
            $"returnRotation={TextTypingSession.ReturnRotation.eulerAngles} entryPointId={TextTypingSession.EntryPointId}"
        );
    }

    private void Log(string message)
    {
        if (verboseLogs)
        {
            Debug.Log("[ReturnSpawnApplier] " + message);
        }
    }
}