using System.Collections;
using UnityEngine;

public class VidaPersonaje : MonoBehaviour
{
    [Header("Vidas")]
    [SerializeField] private int vidasMaximas = 3;
    [SerializeField] private int vidasActuales = 3;

    [Header("UI de vidas")]
    [SerializeField] private VidasUI vidasUI;

    [Header("Game Over")]
    [SerializeField] private GameObject canvasGameOver;

    [Header("Invulnerabilidad")]
    [SerializeField] private float tiempoInvulnerable = 1f;

    private bool estaInvulnerable;
    private bool juegoTerminado;

    public int VidasActuales => vidasActuales;
    public int VidasMaximas => vidasMaximas;

    private void Start()
    {
        Time.timeScale = 1f;

        vidasActuales = vidasMaximas;
        vidasUI.ActualizarVidas(vidasActuales);

        canvasGameOver.SetActive(false);
    }

    public void RecibirDano(int cantidadDano)
    {
        if (estaInvulnerable || juegoTerminado)
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
        juegoTerminado = true;

        canvasGameOver.SetActive(true);

        Time.timeScale = 0f;

        Debug.Log("Game Over");
    }
}