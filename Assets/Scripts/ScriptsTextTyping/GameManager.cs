using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class MathGameManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text questionText;

    [SerializeField] private Button[] answerButtons;
    [SerializeField] private TMP_Text[] answerTexts;
    [SerializeField] private Animator[] answerAnimators; //NUEVO

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

        int num1 = Random.Range(10, 100);
        int num2 = Random.Range(10, 100);

        bool isAddition = Random.value > 0.5f;

        if (isAddition)
        {
            correctAnswer = num1 + num2;
            questionText.text = num1 + " + " + num2;
        }
        else
        {
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
            int index = i;

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
            answerButtons[i].onClick.AddListener(() => SelectAnswer(capturedAnswer, index));
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

        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            int temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }

        return list;
    }

    void SelectAnswer(int selected, int index)
    {
        if (timerScript == null || timerScriptFinished()) return;

        if (scoreManager == null)
        {
            Debug.LogError("ScoreManager NO asignado en GameManager");
            return;
        }

        if (selected == correctAnswer)
        {
            scoreManager.CorrectAnswer();

            // EXPLOSIÓN SOLO EN LA CORRECTA
            if (answerAnimators != null && index < answerAnimators.Length)
            {
                if (answerAnimators[index] != null)
                {
                    answerAnimators[index].SetTrigger("explotar");
                }
                else
                {
                    Debug.LogError("Animator NULL en índice: " + index);
                }
            }

            //Esperar para que se vea la animación
            Invoke(nameof(GenerateQuestion), 0.5f);
        }
        else
        {
            scoreManager.WrongAnswer();
        }
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