using System.Collections;
using UnityEngine;

public class VidaPersonaje : MonoBehaviour
{
    [Header("Vidas")]
    [SerializeField] private int vidasMaximas = 3;
    [SerializeField] private int vidasActuales = 3;

    [Header("UI")]
    [SerializeField] private VidasUI vidasUI;

    [Header("Invulnerabilidad")]
    [SerializeField] private float tiempoInvulnerable = 1f;

    private bool estaInvulnerable;

    private void Start()
    {
        vidasActuales = vidasMaximas;
        vidasUI.ActualizarVidas(vidasActuales);
    }

    public void RecibirDano(int cantidadDano)
    {
        if (estaInvulnerable)
        {
            return;
        }

        vidasActuales -= cantidadDano;

        if (vidasActuales < 0)
        {
            vidasActuales = 0;
        }

        vidasUI.ActualizarVidas(vidasActuales);

        if (vidasActuales <= 0)
        {
            Morir();
        }
        else
        {
            StartCoroutine(InvulnerabilidadTemporal());
        }
    }

    private IEnumerator InvulnerabilidadTemporal()
    {
        estaInvulnerable = true;

        yield return new WaitForSeconds(tiempoInvulnerable);

        estaInvulnerable = false;
    }

    private void Morir()
    {
        Debug.Log("El personaje se quedó sin vidas");
        gameObject.SetActive(false);
    }
}