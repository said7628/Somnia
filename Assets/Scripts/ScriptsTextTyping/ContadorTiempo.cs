using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class Timer : MonoBehaviour
{
    [SerializeField] private TMP_Text TimeText;

    private float tiempo = 180f; // 3 minutos

    void Update()
    {
        // Restar tiempo
        tiempo -= Time.deltaTime;

        // Evitar negativos
        if (tiempo < 0)
            tiempo = 0;

        // Convertir a minutos y segundos
        int minutos = Mathf.FloorToInt(tiempo / 60);
        int segundos = Mathf.FloorToInt(tiempo % 60);

        // Mostrar en pantalla
        TimeText.text = "Time: " + minutos.ToString("00") + ":" + segundos.ToString("00");

        // Cuando llegue a 0
        //if (tiempo <= 0)
        //{
            //SceneManager.LoadScene(2);  NO SE CAMBIA HASTA SABER CUAL ES LA ESCENA DEL SCORE AL FINAL DEL NIVEL
        //}
    }
}
