using UnityEngine;

public static class TextTypingYatzisCalculator
{
    private const float MaxReferenceScore = 4000f;

    public static int CalcularComponenteExtraRedondeada(int score, int edad)
    {
        float porcentajeExtra = Mathf.Max(0f, score) * 100f / MaxReferenceScore;
        float factorEdad = edad >= 7 && edad <= 10 ? 50f : 500f;
        float yatzisExtra = (porcentajeExtra / 100f) * factorEdad;
        return Mathf.CeilToInt(yatzisExtra);
    }
}