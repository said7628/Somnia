using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    private const int PointsPerCorrectAnswer = 50;
    private const int PenaltyPerWrongAnswer = 20;

    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text multiplierText;
    [SerializeField] private Animator multiplicadorAnimator;

    private int score;
    private int streak;
    private int currentMultiplier = 1;
    private int mistakes;

    private static readonly int TriggerCorrecto = Animator.StringToHash("cambioMultiplicador");
    private static readonly int TriggerFallo = Animator.StringToHash("falloMultiplicador");

    private void Start()
    {
        score = 0;
        streak = 0;
        currentMultiplier = 1;
        mistakes = 0;
        TextTypingSession.WasPerfectRun = true;
        UpdateUI();
    }

    public void CorrectAnswer()
    {
        streak++;
        currentMultiplier = ResolveMultiplier(streak);
        score += PointsPerCorrectAnswer * currentMultiplier;

        UpdateUI();

        if (multiplicadorAnimator != null)
        {
            multiplicadorAnimator.ResetTrigger(TriggerFallo);
            multiplicadorAnimator.SetTrigger(TriggerCorrecto);
        }
    }

    public void WrongAnswer()
    {
        score = Mathf.Max(0, score - PenaltyPerWrongAnswer);
        streak = 0;
        currentMultiplier = 1;
        mistakes++;
        TextTypingSession.WasPerfectRun = false;

        UpdateUI();

        if (multiplicadorAnimator != null)
        {
            multiplicadorAnimator.ResetTrigger(TriggerCorrecto);
            multiplicadorAnimator.SetTrigger(TriggerFallo);
        }
    }

    private void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }

        if (multiplierText != null)
        {
            multiplierText.text = $"x{currentMultiplier}";
        }
    }

    private static int ResolveMultiplier(int currentStreak)
    {
        if (currentStreak >= 200) return 200;
        if (currentStreak >= 150) return 100;
        if (currentStreak >= 100) return 50;
        if (currentStreak >= 75) return 30;
        if (currentStreak >= 50) return 20;
        if (currentStreak >= 30) return 10;
        if (currentStreak >= 15) return 5;
        if (currentStreak >= 5) return 2;
        return 1;
    }

    public int GetStreak()
    {
        return streak;
    }

    public int GetMultiplier()
    {
        return currentMultiplier;
    }

    public int GetScore()
    {
        return score;
    }

    public int GetMistakes()
    {
        return mistakes;
    }
}