using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Plataforma1PlayerSetup : MonoBehaviour
{
    [Header("Scene references (optional)")]
    [SerializeField] private Transform realPlayer;
    [SerializeField] private GameObject testPlayer;
    [SerializeField] private VidasUI vidasUI;
    [SerializeField] private GameObject canvasGameOver;

    private void Awake()
    {
        if (!string.Equals(SceneManager.GetActiveScene().name, "Plataforma1", StringComparison.Ordinal))
        {
            return;
        }

        Transform player = ResolveRealPlayer();
        if (player == null)
        {
            Debug.LogError("[Plataforma1Setup][ERROR] Could not resolve real Player");
            return;
        }

        Debug.Log($"[Plataforma1Setup] Real Player resolved: {player.name}");

        DisableTestPlayer();
        EnsurePlayerPhysics(player.gameObject);
        EnsureMovement(player.gameObject);
        EnsureLives(player.gameObject);
        AssignCameraTargets(player);
    }

    private Transform ResolveRealPlayer()
    {
        if (realPlayer != null)
        {
            return realPlayer;
        }

        PlayerVisual visual = FindFirstObjectByType<PlayerVisual>(FindObjectsInactive.Include);
        if (visual != null)
        {
            return visual.transform;
        }

        GameObject tagged = GameObject.FindGameObjectWithTag("Player");
        return tagged != null ? tagged.transform : null;
    }

    private void DisableTestPlayer()
    {
        GameObject resolved = testPlayer;
        if (resolved == null)
        {
            Transform byName = transform.root.Find("Prueba(borrar)");
            if (byName != null)
            {
                resolved = byName.gameObject;
            }
        }

        if (resolved != null && resolved.activeSelf)
        {
            resolved.SetActive(false);
            Debug.Log("[Plataforma1Setup] Disabled test player Prueba(borrar)");
        }
    }

    private void EnsurePlayerPhysics(GameObject player)
    {
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("[Plataforma1Setup][ERROR] Missing Rigidbody2D on Player");
        }
        else
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 5f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        Collider2D col = player.GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogError("[Plataforma1Setup][ERROR] Missing Collider2D on Player");
        }
    }

    private void EnsureMovement(GameObject player)
    {
        MoverConImputAction mover = player.GetComponent<MoverConImputAction>();
        if (mover == null)
        {
            return;
        }

        Transform detector = player.transform.Find("DetectorSuelo");
        if (detector == null)
        {
            Debug.LogError("[Plataforma1Setup][ERROR] Missing DetectorSuelo on Player");
        }

        SetPrivateField(mover, "controlMode", PlayerControlMode.Platformer);
        SetPrivateField(mover, "velocidadX", 7f);
        SetPrivateField(mover, "velocidadY", 7f);
        SetPrivateField(mover, "fuerzaSalto", 20f);
        SetPrivateField(mover, "detectorSuelo", detector);
        SetPrivateField(mover, "radioDetectorSuelo", 0.2f);

        Debug.Log("[Plataforma1Setup] Movement mode set to Platformer");
    }

    private void EnsureLives(GameObject player)
    {
        VidaPersonaje vida = player.GetComponent<VidaPersonaje>();
        if (vida == null)
        {
            return;
        }

        VidasUI resolvedVidas = vidasUI != null ? vidasUI : FindFirstObjectByType<VidasUI>(FindObjectsInactive.Include);
        GameObject resolvedGameOver = canvasGameOver != null ? canvasGameOver : GameObject.Find("GameOver");

        if (resolvedVidas == null)
        {
            Debug.LogError("[Plataforma1Setup][ERROR] Missing Vidas UI reference");
        }

        if (resolvedGameOver == null)
        {
            Debug.LogError("[Plataforma1Setup][ERROR] Missing GameOver reference");
        }

        SetPrivateField(vida, "vidasMaximas", 3);
        SetPrivateField(vida, "vidasActuales", 3);
        SetPrivateField(vida, "tiempoInvulnerable", 1f);
        SetPrivateField(vida, "vidasUI", resolvedVidas);
        SetPrivateField(vida, "canvasGameOver", resolvedGameOver);

        Debug.Log("[Plataforma1Setup] Lives assigned to real Player");
    }

    private void AssignCameraTargets(Transform player)
    {
        bool assigned = false;

        foreach (CameraFollow follow in FindObjectsByType<CameraFollow>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            SetPrivateField(follow, "target", player);
            assigned = true;
        }

        foreach (CameraFollowEdgeLimits follow in FindObjectsByType<CameraFollowEdgeLimits>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            SetPrivateField(follow, "jugador", player);
            assigned = true;
        }

        if (assigned)
        {
            Debug.Log("[Plataforma1Setup] Camera target set to real Player");
        }
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        if (target == null)
        {
            return;
        }

        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(target, value);
    }
}