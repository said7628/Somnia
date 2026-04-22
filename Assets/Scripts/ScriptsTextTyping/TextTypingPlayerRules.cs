public static class TextTypingPlayerRules
{
    public static int ObtenerPuntajeMinimoPorEdad(int edad)
    {
        if (edad >= 7 && edad <= 11)
            return 1000;

        if (edad > 11)
            return 1700;

        return 1000;
    }

    public static bool PasoNivel(int edad, int score)
    {
        return score >= ObtenerPuntajeMinimoPorEdad(edad);
    }
}