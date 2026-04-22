using UnityEngine;

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

    public static int ObtenerBonoPerfectoPorIsla(string sourceIslandSceneName, int levelId)
    {
        int islandId = ResolverIsla(sourceIslandSceneName, levelId);

        switch (islandId)
        {
            case 1:
                return 50;
            case 2:
                return 55;
            case 3:
                return 65;
            default:
                return 50;
        }
    }

    private static int ResolverIsla(string sourceIslandSceneName, int levelId)
    {
        if (!string.IsNullOrWhiteSpace(sourceIslandSceneName))
        {
            string normalized = sourceIslandSceneName.Trim().ToLowerInvariant();

            if (normalized.Contains("isla 3") || normalized.Contains("isla3") || normalized.Contains("mapa3"))
                return 3;

            if (normalized.Contains("isla 2") || normalized.Contains("isla2") || normalized.Contains("mapa2"))
                return 2;

            if (normalized.Contains("isla 1") || normalized.Contains("isla1") || normalized.Contains("mapa"))
                return 1;
        }

        int normalizedLevelId = Mathf.Max(1, levelId);
        if (normalizedLevelId <= 2)
            return 1;

        if (normalizedLevelId <= 4)
            return 2;

        return 3;
    }
}