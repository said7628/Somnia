using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerControlMode
{
    Map,
    TextTyping,
    Platformer
}

public class MoverConImputAction : MonoBehaviour
{
    [SerializeField] private InputAction accionMover;
    [SerializeField] private InputAction accionSalto;

    [SerializeField] private float velocidadX = 7f;
    [SerializeField] private float velocidadY = 7f;
    [SerializeField] private float fuerzaSalto = 10f;

    [SerializeField] private PlayerControlMode controlMode = PlayerControlMode.Map;

    [Header("Detección de suelo")]
    [SerializeField] private Transform detectorSuelo;
    [SerializeField] private float radioDetectorSuelo = 0.2f;
    [SerializeField] private LayerMask groundLayers;
    [SerializeField] private int maxJumps = 2;
    [SerializeField] private float groundedVelocityThreshold = 0.05f;

    private Rigidbody2D rb;
    private Collider2D playerCollider;
    private Collider2D detectorSueloCollider;
    private PlayerVisual playerVisual;
    private int jumpsRemaining;
    private bool wasGrounded;

    private void OnEnable()
    {
        accionMover.Enable();
        accionSalto.Enable();
    }

    private void OnDisable()
    {
        accionMover.Disable();
        accionSalto.Disable();
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        playerVisual = GetComponent<PlayerVisual>();
        jumpsRemaining = maxJumps;
        wasGrounded = false;

        ApplyIdleVisual();

        Debug.Log($"[PlayerMovementMode] Mode set={controlMode}");
        Debug.Log($"[PlatformerMovement] isGrounded=false jumpsRemaining={jumpsRemaining}");
    }

    private void Update()
    {
        if (rb == null)
        {
            return;
        }

        Vector2 moveInput = accionMover.ReadValue<Vector2>();

        switch (controlMode)
        {
            case PlayerControlMode.TextTyping:
                rb.linearVelocity = Vector2.zero;
                playerVisual?.SetMovementState(false, Vector2.down);
                break;

            case PlayerControlMode.Platformer:
                HandlePlatformer(moveInput);
                break;

            default:
                HandleMap(moveInput);
                break;
        }
    }

    public void SetControlMode(PlayerControlMode mode)
    {
        if (controlMode == mode)
        {
            return;
        }

        controlMode = mode;

        Debug.Log($"[PlayerMovementMode] Mode set={controlMode}");

        if (mode == PlayerControlMode.TextTyping)
        {
            rb.linearVelocity = Vector2.zero;
            playerVisual?.SetMovementState(false, Vector2.down);
        }
    }

    private void HandleMap(Vector2 moveInput)
    {
        Vector2 normalized = moveInput.normalized;

        rb.linearVelocity = new Vector2(
            normalized.x * velocidadX,
            normalized.y * velocidadY
        );

        bool isMoving = normalized.sqrMagnitude > 0.001f;

        playerVisual?.SetMovementState(isMoving, normalized);
    }

    private void HandlePlatformer(Vector2 moveInput)
    {
        bool isGrounded = CheckGrounded();
        Debug.Log($"[PlatformerMovement] isGrounded={isGrounded} wasGrounded={wasGrounded} jumpsRemaining={jumpsRemaining}");

        if (isGrounded && !wasGrounded && rb.linearVelocity.y <= groundedVelocityThreshold)
        {
            jumpsRemaining = maxJumps;
            Debug.Log("[PlatformerMovement] Landed, jumps reset");
        }

        float horizontal = Mathf.Clamp(moveInput.x, -1f, 1f);

        rb.linearVelocity = new Vector2(
            horizontal * velocidadX,
            rb.linearVelocity.y
        );

        bool jumpPressed = accionSalto.WasPressedThisFrame() || Keyboard.current?.spaceKey.wasPressedThisFrame == true;
        Debug.Log($"[PlatformerMovement] jumpPressed={jumpPressed}");

        if (jumpPressed)
        {
            TryJump();
        }

        wasGrounded = isGrounded;

        bool isMovingHorizontally = Mathf.Abs(horizontal) > 0.01f;

        playerVisual?.SetMovementState(
            isMovingHorizontally,
            new Vector2(horizontal, 0f)
        );
    }

    private bool CheckGrounded()
    {
        if (detectorSuelo == null)
        {
            Debug.Log("[PlatformerMovement][ERROR] DetectorSuelo is missing");
            return false;
        }

        Debug.Log($"[PlatformerMovement] detectorSuelo={detectorSuelo.name} position={detectorSuelo.position} radius={radioDetectorSuelo}");
        Debug.Log($"[PlatformerMovement] Ground check using DetectorSuelo collider={detectorSueloCollider != null}");

        if (detectorSueloCollider != null)
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.useTriggers = false;

            if (groundLayers.value != 0)
            {
                filter.SetLayerMask(groundLayers);
                filter.useLayerMask = true;
            }

            Collider2D[] overlapResults = new Collider2D[8];
            int count = detectorSueloCollider.Overlap(filter, overlapResults);

            for (int i = 0; i < count; i++)
            {
                Collider2D hit = overlapResults[i];

                if (!IsValidGroundHit(hit))
                {
                    continue;
                }

                Debug.Log($"[PlatformerMovement] Ground hit={hit.name} layer={LayerMask.LayerToName(hit.gameObject.layer)}");
                return true;
            }
        }

        int fallbackMask = groundLayers.value == 0 ? Physics2D.DefaultRaycastLayers : groundLayers;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(
            detectorSuelo.position,
            radioDetectorSuelo,
            fallbackMask
        );

        foreach (Collider2D collider in colliders)
        {
            if (!IsValidGroundHit(collider))
            {
                continue;
            }

            Debug.Log($"[PlatformerMovement] Ground hit={collider.name} layer={LayerMask.LayerToName(collider.gameObject.layer)}");
            return true;
        }

        return false;
    }

    private bool IsValidGroundHit(Collider2D collider)
    {
        if (collider == null || collider.isTrigger)
        {
            return false;
        }

        if (collider.transform == transform || collider.transform.IsChildOf(transform))
        {
            return false;
        }

        if (playerCollider != null && collider == playerCollider)
        {
            return false;
        }

        if (collider.attachedRigidbody != null && collider.attachedRigidbody.gameObject == gameObject)
        {
            return false;
        }

        return true;
    }

    private void TryJump()
    {
        if (rb == null)
        {
            Debug.Log("[PlatformerMovement][ERROR] Rigidbody2D missing");
            return;
        }

        if (jumpsRemaining <= 0)
        {
            Debug.Log("[PlatformerMovement][WARN] Jump pressed but no jumps remaining");
            return;
        }

        Vector2 velocity = rb.linearVelocity;
        velocity.y = 0f;
        rb.linearVelocity = velocity;

        rb.AddForce(Vector2.up * fuerzaSalto, ForceMode2D.Impulse);
        jumpsRemaining--;

        Debug.Log($"[PlatformerMovement] Jump applied force={fuerzaSalto} jumpsRemaining={jumpsRemaining}");
    }

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }
        if (playerCollider == null)
        {
            playerCollider = GetComponent<Collider2D>();
        }

        if (rb == null)
        {
            Debug.Log("[PlatformerMovement][ERROR] Rigidbody2D missing");
        }

        if (detectorSuelo == null)
        {
            Debug.Log("[PlatformerMovement][ERROR] DetectorSuelo is missing");
        }
        else
        {
            detectorSueloCollider = detectorSuelo.GetComponent<Collider2D>();
        }

        jumpsRemaining = maxJumps;
        wasGrounded = false;
    }

    private void ApplyIdleVisual()
    {
        playerVisual?.SetMovementState(false, Vector2.down);
    }

    private void OnDrawGizmosSelected()
    {
        if (detectorSuelo == null)
        {
            return;
        }

        Gizmos.DrawWireSphere(detectorSuelo.position, radioDetectorSuelo);
    }
}