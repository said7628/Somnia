using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class Timer : MonoBehaviour
{
    private const float FixedDurationSeconds = 150f;

    [FormerlySerializedAs("TimeText")]
    [SerializeField] private TMP_Text timeText;

    private float tiempoRestante;

    private void Awake()
    {
        if (Time.timeScale <= 0f)
        {
            Time.timeScale = 1f;
        }

        tiempoRestante = FixedDurationSeconds;
        UpdateUI();
    }

    private void Update()
    {
        

        if (tiempoRestante <= 0f)
        {
            return;
        }

        tiempoRestante -= Time.deltaTime;

        if (tiempoRestante < 0f)
        {
            tiempoRestante = 0f;
        }

        UpdateUI();
    }

    public float GetTime()
    {
        return tiempoRestante;
    }

    private void UpdateUI()
    {
        if (timeText == null)
        {
            return;
        }

        int minutos = Mathf.FloorToInt(tiempoRestante / 60f);
        int segundos = Mathf.FloorToInt(tiempoRestante % 60f);
        timeText.text = $"{minutos:00}:{segundos:00}";
    }
}