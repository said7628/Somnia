using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ResultadoNivelUI : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreValue;
    [SerializeField] private TMP_Text perfectValue;
    [SerializeField] private TMP_Text monedasValue;
    [SerializeField] private TMP_Text minScoreValue;
    [SerializeField] private TMP_Text estadoValue;

    private void Start()
    {
        if (scoreValue != null)
        {
            scoreValue.text = TextTypingSession.CurrentScore.ToString();
            Debug.Log($"[ScoreTextTyping] scoreValue text={scoreValue.text}");
        }

        if (minScoreValue != null)
        {
            minScoreValue.text = TextTypingSession.MinimumScore.ToString();
        }

        if (estadoValue != null)
        {
            estadoValue.text = TextTypingSession.Passed ? "Completado" : "No completado";
        }

        if (perfectValue != null)
        {
            bool perfect = TextTypingSession.WasPerfectRun;
            int perfectBonus = 0;

            if (perfect)
            {
                perfectBonus = IsIsland2() ? 55 : 50;
            }

            perfectValue.text = $"+{perfectBonus}";
        }


        if (monedasValue != null)
        {
            monedasValue.text = Mathf.Max(0, TextTypingSession.AwardedYatzis).ToString();
        }
        int islandId = TextTypingSession.ResolveIslandId();
        Debug.Log($"[ScoreTextTyping] islandId={islandId} levelId={TextTypingSession.LastLevelId}");
        Debug.Log($"[ScoreTextTyping] perfectBonus={Mathf.Max(0, TextTypingSession.PerfectBonusYatzis)}");
        Debug.Log($"[ScoreTextTyping] totalYatzisEarned={Mathf.Max(0, TextTypingSession.AwardedYatzis)}");
        Debug.Log($"[ScoreTextTyping] perfectValue text={perfectValue.text}");
        Debug.Log($"[ScoreTextTyping] monedasValue text={Mathf.Max(0, TextTypingSession.AwardedYatzis)}");

        Debug.Log($"[TextTypingWin] Earned yatzis: {TextTypingSession.AwardedYatzis}");
        Debug.Log($"[TextTypingWin] Perfect bonus: {TextTypingSession.PerfectBonusYatzis}");

        Debug.Log($"[TextTypingResultSuccess] Source level scene={TextTypingSession.LastLevelSceneName} levelId={TextTypingSession.LastLevelId} sourceIslandScene={TextTypingSession.SourceIslandSceneName}");
        Debug.Log($"[TextTypingResultSuccess] Final score={TextTypingSession.CurrentScore}");
        Debug.Log($"[TextTypingResultSuccess] Previous personal best={TextTypingSession.PreviousPersonalBest}");
        Debug.Log($"[TextTypingResultSuccess] New personal best={TextTypingSession.PersonalBest}");
        Debug.Log($"[TextTypingResultSuccess] Yatzis awarded(current run)={TextTypingSession.AwardedYatzis} total(account)={TextTypingSession.TotalYatzis} persisted={TextTypingSession.RewardSavedInBackend}");
        Debug.Log($"[TextTypingResultSuccess] Save progress completed. passed={TextTypingSession.Passed}");
        Debug.Log($"[TextTypingResultSuccess] Unlock next level evaluation for levelId={TextTypingSession.LastLevelId}");
    }

    private static bool IsIsland2()
    {
        return TextTypingSession.ResolveIslandId() == 2;
    }

    public void IrAMapa()
    {
        string returnScene = TextTypingSession.ResolveReturnScene();
        if (TextTypingSession.Passed)
        {
            GameSceneTransitionContext.PlayLevelPassedAudioOnNextIslandLoad = true;
            GameSceneTransitionContext.PassedLevelId = TextTypingSession.LastLevelId;
        }
        else
        {
            GameSceneTransitionContext.PlayLevelPassedAudioOnNextIslandLoad = false;
            GameSceneTransitionContext.PassedLevelId = -1;
            Debug.Log("[IslandLevelManager] Failed flow detected: no unlock, no passed audio");
        }
        Debug.Log($"[TextTypingResultSuccess] Map return sourceIslandScene={TextTypingSession.SourceIslandSceneName} sourceLevelScene={TextTypingSession.LastLevelSceneName} returnPosition={TextTypingSession.ReturnPosition} returnRotation={TextTypingSession.ReturnRotation.eulerAngles} sceneLoadedByMapReturn={returnScene}");
        SceneManager.LoadScene(returnScene);
    }
}