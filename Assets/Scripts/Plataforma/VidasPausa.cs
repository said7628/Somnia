using UnityEngine;

public class SistemaVidasUI : MonoBehaviour
{
    [Header("Vidas llenas")]
    public GameObject[] vidasLlenas;

    [Header("Vidas máximas")]
    public int vidasMaximas = 3;

    private int vidasActuales;

    void Start()
    {
        vidasActuales = vidasMaximas;
        ActualizarUI();
    }

    public void PerderVida()
    {
        if (vidasActuales > 0)
        {
            vidasActuales--;
            ActualizarUI();
        }

        if (vidasActuales <= 0)
        {
            Debug.Log("GAME OVER");
            // aquí luego conectas tu pantalla de GameOver
        }
    }

    public void GanarVida()
    {
        if (vidasActuales < vidasMaximas)
        {
            vidasActuales++;
            ActualizarUI();
        }
    }

    void ActualizarUI()
    {
        for (int i = 0; i < vidasLlenas.Length; i++)
        {
            if (i < vidasActuales)
                vidasLlenas[i].SetActive(true);
            else
                vidasLlenas[i].SetActive(false);
        }
    }
}