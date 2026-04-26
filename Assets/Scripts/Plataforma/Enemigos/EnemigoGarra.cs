using System.Collections;
using UnityEngine;

public class EnemigoGarraCiclica : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animador;

    [Header("Nombres de animaciones")]
    [SerializeField] private string animacionOculta = "Oculto";
    [SerializeField] private string animacionSalir = "GarraEnemigoSalir";
    [SerializeField] private string animacionAfuera = "Afuera";
    [SerializeField] private string animacionGuardar = "Guardar";

    [Header("Colliders de daño")]
    [SerializeField] private Collider2D[] collidersDano;

    [Header("Daño")]
    [SerializeField] private int dano = 1;

    [Header("Tiempos")]
    [SerializeField] private float tiempoOculta = 2f;
    [SerializeField] private float duracionSalir = 0.5f;
    [SerializeField] private float tiempoAfuera = 2f;
    [SerializeField] private float duracionGuardar = 0.5f;

    private bool puedeHacerDano;

    private void Start()
    {
        ApagarCollidersDano();
        StartCoroutine(CicloGarra());
    }

    private IEnumerator CicloGarra()
    {
        while (true)
        {
            animador.Play(animacionOculta);
            puedeHacerDano = false;
            ApagarCollidersDano();

            yield return new WaitForSeconds(tiempoOculta);

            animador.Play(animacionSalir);
            puedeHacerDano = true;
            EncenderCollidersDano();

            yield return new WaitForSeconds(duracionSalir);

            animador.Play(animacionAfuera);
            puedeHacerDano = true;
            EncenderCollidersDano();

            yield return new WaitForSeconds(tiempoAfuera);

            animador.Play(animacionGuardar);
            EncenderCollidersDano();

            yield return new WaitForSeconds(duracionGuardar);
        }
    }

    private void EncenderCollidersDano()
    {
        for (int i = 0; i < collidersDano.Length; i++)
        {
            collidersDano[i].enabled = true;
        }
    }

    private void ApagarCollidersDano()
    {
        for (int i = 0; i < collidersDano.Length; i++)
        {
            collidersDano[i].enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        HacerDanoAlJugador(otro);
    }

    private void OnTriggerStay2D(Collider2D otro)
    {
        HacerDanoAlJugador(otro);
    }

    private void HacerDanoAlJugador(Collider2D otro)
    {
        if (puedeHacerDano && otro.CompareTag("Jugador"))
        {
            VidaPersonaje vidaPersonaje = otro.GetComponentInParent<VidaPersonaje>();

            if (vidaPersonaje != null)
            {
                vidaPersonaje.RecibirDano(dano);
                puedeHacerDano = false;
            }
        }
    }
}