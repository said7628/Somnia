using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour
{
    [SerializeField] private TMP_Text TimeText;

    private float tiempo = 180f; // 3 minutos

    void Update()
    {
        tiempo -= Time.deltaTime;

        if (tiempo < 0)
            tiempo = 0;

        int minutos = Mathf.FloorToInt(tiempo / 60);
        int segundos = Mathf.FloorToInt(tiempo % 60);

        TimeText.text = minutos.ToString("00") + ":" + segundos.ToString("00");
    }

    // ✅ MÉTODO PÚBLICO (clave para el GameManager)
    public float GetTime()
    {
        return tiempo;
    }
}