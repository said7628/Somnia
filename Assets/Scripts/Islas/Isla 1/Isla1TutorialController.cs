using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Isla1TutorialController : MonoBehaviour
{
    [SerializeField] private SistemaDialogoCanvas tutorialDialogo;
    [SerializeField] private Collider2D tutorialTriggerCollider;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string tutorialPrefsKey = "Isla1_TutorialSeen_v1";

    private bool hasSeenTutorial;

    private void Awake()
    {
        hasSeenTutorial = PlayerPrefs.GetInt(tutorialPrefsKey, 0) == 1;
        Debug.Log($"[Isla1Tutorial] Awake key={tutorialPrefsKey} seen={hasSeenTutorial.ToString().ToLowerInvariant()}");

        if (tutorialDialogo == null)
        {
            Debug.LogWarning("[Isla1Tutorial][WARN] Tutorial dialog reference missing");
            return;
        }

        tutorialDialogo.OnDialogFinished += HandleTutorialFinished;

        if (hasSeenTutorial)
        {
            Debug.Log("[Isla1Tutorial] Tutorial already seen, skipping auto-start");
            Debug.Log("[Isla1Tutorial] Ensuring tutorial dialog/audio is stopped");
            tutorialDialogo.SetAutoActivationEnabled(false);
            tutorialDialogo.StopDialogCompletely();
            DisableTutorialTriggerCollider();
        }
    }

    private IEnumerator Start()
    {
        if (tutorialDialogo == null || hasSeenTutorial)
        {
            yield break;
        }

        yield return null;
        Debug.Log("[Isla1Tutorial] First-time tutorial auto-start");
        TryStartTutorial();
    }

    private void OnDestroy()
    {
        if (tutorialDialogo != null)
        {
            tutorialDialogo.OnDialogFinished -= HandleTutorialFinished;
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.qKey.wasPressedThisFrame)
        {
            return;
        }

        Debug.Log("[Isla1Tutorial] Q pressed, replaying tutorial");
        if (tutorialDialogo != null)
        {
            tutorialDialogo.SetAutoActivationEnabled(false);
        }
        TryStartTutorial();
    }

    private void TryStartTutorial()
    {
        if (tutorialDialogo == null)
        {
            Debug.LogWarning("[Isla1Tutorial][WARN] Tutorial dialog reference missing");
            return;
        }

        if (tutorialDialogo.IsDialogActive)
        {
            Debug.LogWarning("[Isla1Tutorial][WARN] Tutorial already active, ignoring duplicate request");
            return;
        }

        tutorialDialogo.ActivarDialogo();
    }

    private void HandleTutorialFinished()
    {
        if (!hasSeenTutorial)
        {
            hasSeenTutorial = true;
            PlayerPrefs.SetInt(tutorialPrefsKey, 1);
            PlayerPrefs.Save();
            Debug.Log("[Isla1Tutorial] Tutorial completed, saved seen=true");
        }

        DisableTutorialTriggerCollider();
    }

    private void DisableTutorialTriggerCollider()
    {
        if (tutorialTriggerCollider == null)
        {
            return;
        }

        if (!tutorialTriggerCollider.enabled)
        {
            return;
        }

        tutorialTriggerCollider.enabled = false;
        Debug.Log("[Isla1Tutorial] Tutorial trigger collider disabled");
    }
}