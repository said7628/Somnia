using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;


public class MathGameManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text questionText;

    [SerializeField] private Button[] answerButtons;
    [SerializeField] private TMP_Text[] answerTexts;
    [SerializeField] private Animator[] answerAnimators; 

    [Header("Managers")]
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private Timer timerScript;

    private int correctAnswer;

    public enum GameMode
    {
        SumaResta,
        MultDiv,
        Patrones,
        Sucesiones,
        MayorMenor,
        Mixto
    }
    private GameMode gameMode;
    void Start()
    {
        Debug.Log("ESCENA ACTUAL: " + SceneManager.GetActiveScene().name);
        Debug.Log("INICIANDO JUEGO");

        SetGameModeByScene();

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
    switch (gameMode)
    {
        case GameMode.SumaResta:
            GenerateSumaResta();
            break;

        case GameMode.MultDiv:
            GenerateMultDiv();
            break;

        case GameMode.Patrones:
            GeneratePatrones();
            break;

        case GameMode.Sucesiones:
            GenerateSucesiones();
            break;

        case GameMode.MayorMenor:
            GenerateMayorMenor();
            break;

        case GameMode.Mixto:
            GenerateMixto();
            break;
    }
    Debug.Log("USANDO MODO: " + gameMode);
}

void GenerateSumaResta()
{
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

    SetupAnswers(correctAnswer);
}

void GenerateMultDiv()
{
    int num1 = Random.Range(2, 20);
    int num2 = Random.Range(2, 20);

    bool isMult = Random.value > 0.5f;

    if (isMult)
    {
        correctAnswer = num1 * num2;
        questionText.text = num1 + " × " + num2;
    }
    else
    {
        correctAnswer = num1;
        int result = num1 * num2;

        questionText.text = result + " ÷ " + num2;
    }

    SetupAnswers(correctAnswer);
}

void GeneratePatrones()
{
    int start = Random.Range(1, 20);
    int step = Random.Range(2, 10);

    int a = start;
    int b = a + step;
    int c = b + step;
    int d = c + step;

    correctAnswer = d + step;

    questionText.text = a + ", " + b + ", " + c + ", " + d + ", ?";

    SetupAnswers(correctAnswer);
}

void GenerateSucesiones()
{
    int a = Random.Range(1, 10);
    int b = Random.Range(1, 10);

    int c = a + b;
    int d = b + c;

    correctAnswer = c + d;

    questionText.text = a + ", " + b + ", " + c + ", " + d + ", ?";

    SetupAnswers(correctAnswer);
}

void GenerateMayorMenor()
{
    int a = Random.Range(10, 100);
    int b = Random.Range(10, 100);

    bool askGreater = Random.value > 0.5f;

    if (askGreater)
    {
        correctAnswer = Mathf.Max(a, b);
        questionText.text = "¿Cuál es MAYOR?\n" + a + " o " + b;
    }
    else
    {
        correctAnswer = Mathf.Min(a, b);
        questionText.text = "¿Cuál es MENOR?\n" + a + " o " + b;
    }

    // Generar respuestas más lógicas
    List<int> answers = new List<int>();
    answers.Add(a);
    answers.Add(b);

    while (answers.Count < 8)
    {
        int fake = Random.Range(10, 100);
        if (!answers.Contains(fake))
            answers.Add(fake);
    }

    ShuffleAndAssign(answers);
}

void GenerateMixto()
{
    if (Random.value > 0.5f)
        GenerateSumaResta();
    else
        GenerateMultDiv();
}

void SetupAnswers(int correct)
{
    List<int> answers = GenerateAnswers(correct);

    for (int i = 0; i < answerButtons.Length; i++)
    {
        int capturedAnswer = answers[i];
        int index = i;

        answerTexts[i].text = capturedAnswer.ToString();

        answerButtons[i].onClick.RemoveAllListeners();
        answerButtons[i].onClick.AddListener(() => SelectAnswer(capturedAnswer, index));
    }
}
void ShuffleAndAssign(List<int> answers)
{
    for (int i = 0; i < answers.Count; i++)
    {
        int randomIndex = Random.Range(i, answers.Count);
        int temp = answers[i];
        answers[i] = answers[randomIndex];
        answers[randomIndex] = temp;
    }

    for (int i = 0; i < answerButtons.Length; i++)
    {
        int capturedAnswer = answers[i];
        int index = i;

        answerTexts[i].text = capturedAnswer.ToString();

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
                    Debug.Log("💥 Activando animación en botón: " + index);
                    answerAnimators[index].Play("Explode", 0, 0f);
                }
                else
                {
                    Debug.LogError("Animator NULL en índice: " + index);
                }
            }

            //Esperar para que se vea la animación
            Invoke(nameof(GenerateQuestion), 1f);
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

    void SetGameModeByScene()
{
    string sceneName = SceneManager.GetActiveScene().name;

    switch (sceneName)
    {
        case "TextTyping":
            gameMode = GameMode.SumaResta;
            break;

        case "TextTyping2":
            gameMode = GameMode.MultDiv;
            break;

        case "TextTyping3":
            gameMode = GameMode.Patrones;
            break;

        case "TextTyping4":
            gameMode = GameMode.Sucesiones;
            break;

        case "TextTyping5":
            gameMode = GameMode.MayorMenor;
            break;

        case "TextTyping6":
            gameMode = GameMode.Mixto;
            break;

        default:
            Debug.LogWarning("Escena no reconocida, usando SumaResta por defecto");
            gameMode = GameMode.SumaResta;
            break;
    }

    Debug.Log("Modo de juego: " + gameMode);
}

}