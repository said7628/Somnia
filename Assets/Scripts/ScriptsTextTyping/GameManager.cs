using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class MathGameManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private TMP_Text streakText;

    [SerializeField] private Button[] answerButtons;
    [SerializeField] private TMP_Text[] answerTexts; // 🔥 NUEVO (mejor que GetComponentInChildren)

    [Header("Managers")]
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private Timer timerScript;

    private int correctAnswer;

    void Start()
    {
        Debug.Log("INICIANDO JUEGO");
        GenerateQuestion();
    }

    void Update()
    {
        if (timerScript != null && timerScriptFinished())
        {
            Debug.Log("TIEMPO TERMINADO");
            DisableAllButtons();
        }
    }

    bool timerScriptFinished()
    {
        return timerScript.GetTime() <= 0;
    }

    void GenerateQuestion()
    {
        Debug.Log("=== GENERANDO NUEVA PREGUNTA ===");

        int num1 = Random.Range(100, 1000);
        int num2 = Random.Range(100, 1000);

        bool isAddition = Random.value > 0.5f;

        if (isAddition)
        {
        correctAnswer = num1 + num2;
        questionText.text = num1 + " + " + num2;
        }
        else
        {
            // Asegurar que no haya negativos
            if (num2 > num1)
            {
                int temp = num1;
                num1 = num2;
                num2 = temp;
            }

    correctAnswer = num1 - num2;
    questionText.text = num1 + " - " + num2;
}

        Debug.Log("Pregunta: " + questionText.text);
        Debug.Log("Respuesta correcta: " + correctAnswer);

        List<int> answers = GenerateAnswers(correctAnswer);

        for (int i = 0; i < answerButtons.Length; i++)
        {
            int capturedAnswer = answers[i];

            // 🔥 USAMOS ARRAY DIRECTO (más seguro)
            if (i < answerTexts.Length)
            {
                answerTexts[i].text = capturedAnswer.ToString();
            }
            else
            {
                Debug.LogError("Falta asignar answerTexts en el índice: " + i);
            }

            Debug.Log("Botón " + i + ": " + capturedAnswer);

            answerButtons[i].onClick.RemoveAllListeners();
            answerButtons[i].onClick.AddListener(() => SelectAnswer(capturedAnswer));
        }
    }

    List<int> GenerateAnswers(int correct)
    {
        HashSet<int> answers = new HashSet<int>();
        answers.Add(correct);

        while (answers.Count < 8)
        {
            int fake = correct + Random.Range(-200, 200);

            if (fake != correct)
                answers.Add(fake);
        }

        List<int> list = new List<int>(answers);

        // Mezclar
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            int temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }

        return list;
    }

void SelectAnswer(int selected)
{
    Debug.Log("CLICK DETECTADO: " + selected);

    Debug.Log("scoreManager: " + scoreManager);
    Debug.Log("streakText: " + streakText);
    Debug.Log("timerScript: " + timerScript);

    if (timerScript == null || timerScriptFinished()) return;

    if (selected == correctAnswer)
    {
        scoreManager.CorrectAnswer();
    }
    else
    {
        scoreManager.WrongAnswer();
    }

    if (streakText != null)
        streakText.text = "Racha: " + scoreManager.GetStreak();
    else
        Debug.LogError("streakText NO asignado");

    GenerateQuestion();
}

    void DisableAllButtons()
    {
        Debug.Log("Desactivando botones");

        foreach (Button btn in answerButtons)
        {
            btn.interactable = false;
        }
    }
}