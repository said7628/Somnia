using UnityEngine;

public static class TextTypingPlayerRules
{
    public static int ObtenerPuntajeMinimoPorEdad(int edad)
    {
        if (edad >= 7 && edad <= 10)
            return 200;

        if (edad >= 11)
            return 500;

        return 500;
    }

    public static bool PasoNivel(int edad, int score)
    {
        return score >= ObtenerPuntajeMinimoPorEdad(edad);
    }
}