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
    [SerializeField] private int maxJumps = 2;

    private Rigidbody2D rb;
    private PlayerVisual playerVisual;
    private int jumpsRemaining;

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
        playerVisual = GetComponent<PlayerVisual>();
        jumpsRemaining = maxJumps;

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
        bool isGrounded = EstaTocandoSuelo();
        Debug.Log($"[PlatformerMovement] isGrounded={isGrounded} jumpsRemaining={jumpsRemaining}");

        if (isGrounded && jumpsRemaining != maxJumps)
        {
            jumpsRemaining = maxJumps;
            Debug.Log("[PlatformerMovement] Grounded, jumps reset");
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

        bool isMovingHorizontally = Mathf.Abs(horizontal) > 0.01f;

        playerVisual?.SetMovementState(
            isMovingHorizontally,
            new Vector2(horizontal, 0f)
        );
    }

    private bool EstaTocandoSuelo()
    {
        if (detectorSuelo == null)
        {
            Debug.Log("[PlatformerMovement][ERROR] DetectorSuelo missing");
            return false;
        }

        Debug.Log($"[PlatformerMovement] detectorSuelo={detectorSuelo.name} position={detectorSuelo.position} radius={radioDetectorSuelo}");

        Collider2D[] colliders = Physics2D.OverlapCircleAll(
            detectorSuelo.position,
            radioDetectorSuelo
        );

        foreach (Collider2D collider in colliders)
        {
            if (collider == null)
            {
                continue;
            }

            if (collider.attachedRigidbody != null && collider.attachedRigidbody.gameObject == gameObject)
            {
                continue;
            }

            if (collider.gameObject == gameObject)
            {
                continue;
            }

            return true;
        }

        return false;
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

        if (rb == null)
        {
            Debug.Log("[PlatformerMovement][ERROR] Rigidbody2D missing");
        }

        if (detectorSuelo == null)
        {
            Debug.Log("[PlatformerMovement][ERROR] DetectorSuelo missing");
        }

        jumpsRemaining = maxJumps;
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