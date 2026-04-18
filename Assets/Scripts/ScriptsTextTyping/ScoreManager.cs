using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text multiplierText;
    [SerializeField] private Animator multiplicadorAnimator;

    private int score = 0;
    private int streak = 0;
    private int multiplier = 1;

    private const int basePoints = 50;

    void Start()
    {
        UpdateUI();
    }

public void CorrectAnswer()
{
    streak++;

    int oldMultiplier = multiplier;

    UpdateMultiplier();

    Debug.Log("Streak: " + streak);
    Debug.Log("Old Mult: " + oldMultiplier);
    Debug.Log("New Mult: " + multiplier);

    int pointsEarned = basePoints * multiplier;
    score += pointsEarned;

    UpdateUI();

    if (multiplier > oldMultiplier)
    {
        Debug.Log("🔥 CAMBIO DE MULTIPLICADOR → ANIMACION");

        if (multiplicadorAnimator != null)
        {
            multiplicadorAnimator.SetTrigger("cambioMultiplicador");
        }
        else
        {
            Debug.LogError("Animator NO asignado");
        }
    }
}

    public void WrongAnswer()
    {
        streak = 0;
        multiplier = 1;

        UpdateUI();
    }

    void UpdateMultiplier()
    {
        if (streak >= 200)
            multiplier = 200;
        else if (streak >= 150)
            multiplier = 150;
        else if (streak >= 100)
            multiplier = 30;
        else if (streak >= 75)
            multiplier = 30;
        else if (streak >= 50)
            multiplier = 20;
        else if (streak >= 30)
            multiplier = 10;
        else if (streak >= 15)
            multiplier = 5;
        else if (streak >= 5)
            multiplier = 2;
        else
            multiplier = 1;
    }

    void UpdateUI()
    {
        scoreText.text = score.ToString();
        multiplierText.text = "x" + multiplier;
    }

    // ✅ Getter para la racha
    public int GetStreak()
    {
        return streak;
    }

    public int GetScore()
    {
        return score;
    }
}