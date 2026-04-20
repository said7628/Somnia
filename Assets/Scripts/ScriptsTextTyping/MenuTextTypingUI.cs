using TMPro;
using UnityEngine;
using Somnia.UnityClient;

public class TextTypingMenuRealUI : MonoBehaviour
{
    [SerializeField] private TMP_Text minScoreValue;
    [SerializeField] private TMP_Text maxScoreValue;

    private void Start()
    {
        int minScore = 500;

        if (GameSessionManager.Instance != null && GameSessionManager.Instance.HasSession())
        {
            minScore = GameSessionManager.Instance.GetTextTypingMinimumScore();
        }

        if (minScoreValue != null)
            minScoreValue.text = minScore.ToString();

        if (maxScoreValue != null)
            maxScoreValue.text = TextTypingSession.PersonalBest.ToString();
    }
}