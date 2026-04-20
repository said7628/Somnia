using System.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;

public class MoverConImputAction : MonoBehaviour
{
    [SerializeField]
    private InputAction accionMover;

    [SerializeField]
    private InputAction accionSalto;

    private Rigidbody2D rb;

    private float velocidadX = 7f;
    private float velocidadY = 7f;

    void Start()
    {
        accionMover.Enable();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        Vector2 movimiento = accionMover.ReadValue<Vector2>();

        // 🔥 Manteniendo tu lógica, pero agregando eje Y
        rb.linearVelocityX = velocidadX * movimiento.x;
        rb.linearVelocityY = velocidadY * movimiento.y;
    }
}