using UnityEngine;

public class VidasUI : MonoBehaviour
{
    [Header("Vidas vacías")]
    [SerializeField] private GameObject[] vidasVacias;

    [Header("Vidas llenas")]
    [SerializeField] private GameObject[] vidasLlenas;

    public void ActualizarVidas(int vidasActuales)
    {
        for (int i = 0; i < vidasVacias.Length; i++)
        {
            vidasVacias[i].SetActive(true);
        }

        for (int i = 0; i < vidasLlenas.Length; i++)
        {
            if (i < vidasActuales)
            {
                vidasLlenas[i].SetActive(true);
            }
            else
            {
                vidasLlenas[i].SetActive(false);
            }
        }
    }
}