using UnityEngine;

public class DanoPinchos : MonoBehaviour
{
    [SerializeField] private int dano = 1;

    private void OnTriggerEnter2D(Collider2D otro)
    {
        if (otro.CompareTag("Jugador"))
        {
            VidaPersonaje vidaPersonaje = otro.GetComponentInParent<VidaPersonaje>();

            vidaPersonaje.RecibirDano(dano);
        }
    }

    private void OnTriggerStay2D(Collider2D otro)
    {
        if (otro.CompareTag("Jugador"))
        {
            VidaPersonaje vidaPersonaje = otro.GetComponentInParent<VidaPersonaje>();

            vidaPersonaje.RecibirDano(dano);
        }
    }
}