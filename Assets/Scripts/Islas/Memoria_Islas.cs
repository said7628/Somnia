using UnityEngine;

public class Memoria_Islas : MonoBehaviour
{
    // El post-it para saber de que isla venimos
    public static string islaDeDondeVengo = "Isla1";

    // Fallback local: jugador nuevo inicia siempre en 0 mientras carga backend.
    public static int misMonedas = 0;
}