using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScorePlataformaController : MonoBehaviour
{
    [SerializeField] private TMP_Text baseText;
    [SerializeField] private TMP_Text perfectText;
    [SerializeField] private TMP_Text yatzisText;
    [SerializeField] private TMP_Text completadoText;
    [SerializeField] private Button mapaButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private string menuSceneName = "Menu";

    private void Start()
    {
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        int slot = Mathf.Max(1, PlataformaSession.SlotNumber);
        int levelId = Mathf.Max(1, PlataformaSession.LevelId);

        Debug.Log($"[ScorePlataforma] Loaded session slot={slot} levelId={levelId} sourceScene={PlataformaSession.SourceLevelScene}");
        if (!PlataformaSession.Completed)
        {
            Debug.LogWarning("[ScorePlataforma][WARN] Session completed flag is false, applying safe defaults.");
        }

        int livesRemaining = Mathf.Max(0, PlataformaSession.LivesRemaining);
        int maxLives = Mathf.Max(0, PlataformaSession.MaxLives);
        bool perfect = maxLives > 0 && livesRemaining >= maxLives;
        int baseYatzis = 50;
        int perfectBonus = perfect ? 10 : 0;
        int totalYatzisEarned = Mathf.Clamp(baseYatzis + perfectBonus, 50, 60);
        totalYatzisEarned = totalYatzisEarned >= 60 ? 60 : 50;

        Debug.Log($"[ScorePlataforma] livesRemaining={livesRemaining} maxLives={maxLives} perfect={perfect}");
        Debug.Log($"[ScorePlataforma] baseYatzis=50 perfectBonus={perfectBonus} totalYatzisEarned={totalYatzisEarned}");

        if (baseText != null) baseText.text = "+50";
        if (perfectText != null) perfectText.text = perfectBonus > 0 ? "+10" : "+0";
        if (yatzisText != null) yatzisText.text = totalYatzisEarned.ToString();
        if (completadoText != null) completadoText.text = "¡Completado!";

        Debug.Log($"[ScorePlataforma] UI baseText=+50 perfectText={(perfectBonus > 0 ? "+10" : "+0")} totalText={totalYatzisEarned}");

        if (!PlataformaSession.RewardAlreadySaved)
        {
            try
            {
                await PlataformaProgressService.SaveCompletionRewardAsync(slot, levelId, totalYatzisEarned);
                PlataformaSession.RewardAlreadySaved = true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ScorePlataformaProgress][ERROR] Failed to save Plataforma reward: {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning("[ScorePlataformaProgress][WARN] Reward already saved, skipping duplicate save");
        }

        if (mapaButton != null)
        {
            mapaButton.onClick.RemoveAllListeners();
            mapaButton.onClick.AddListener(OnMapaButtonPressed);
        }

        if (menuButton != null)
        {
            menuButton.onClick.RemoveAllListeners();
            menuButton.onClick.AddListener(OnMenuButtonPressed);
        }
    }

    private void OnMapaButtonPressed()
    {
        string targetScene = string.IsNullOrWhiteSpace(PlataformaSession.ReturnScene) ? "Isla1" : PlataformaSession.ReturnScene;
        SceneManager.LoadScene(targetScene);
    }

    private void OnMenuButtonPressed()
    {
        if (!string.IsNullOrWhiteSpace(menuSceneName))
        {
            SceneManager.LoadScene(menuSceneName);
        }
    }
}