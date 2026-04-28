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

    private Rigidbody2D rb;
    private PlayerVisual playerVisual;

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
        ApplyIdleVisual();
        Debug.Log($"[PlayerMovementMode] Mode set={controlMode}");
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
        rb.linearVelocity = new Vector2(normalized.x * velocidadX, normalized.y * velocidadY);

        bool isMoving = normalized.sqrMagnitude > 0.001f;
        playerVisual?.SetMovementState(isMoving, normalized);
    }

    private void HandlePlatformer(Vector2 moveInput)
    {
        float horizontal = Mathf.Clamp(moveInput.x, -1f, 1f);
        rb.linearVelocity = new Vector2(horizontal * velocidadX, rb.linearVelocity.y);

        bool jumpPressed = accionSalto.WasPressedThisFrame();
        if (jumpPressed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaSalto);
        }

        bool isMovingHorizontally = Mathf.Abs(horizontal) > 0.01f;
        playerVisual?.SetMovementState(isMovingHorizontally, new Vector2(horizontal, 0f));
    }

    private void ApplyIdleVisual()
    {
        playerVisual?.SetMovementState(false, Vector2.down);
    }
}