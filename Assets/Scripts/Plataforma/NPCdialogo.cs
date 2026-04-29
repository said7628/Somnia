using UnityEngine;

public class ActivadorDialogoNPC : MonoBehaviour
{
    [SerializeField] private SistemaDialogoCanvas sistemaDialogo;
    [SerializeField] private string tagJugador = "Jugador";

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(tagJugador))
        {
            sistemaDialogo.ActivarDialogo();
        }
    }
}