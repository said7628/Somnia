using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ResultadoNivelUI : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreValue;
    [SerializeField] private TMP_Text bonusValue;
    [SerializeField] private TMP_Text coinsValue;

    private void Start()
    {
        scoreValue.text = "300";
        bonusValue.text = "+65";
        coinsValue.text = "216";
    }

    public void IrAMapa()
    {
        SceneManager.LoadScene("Mapa");
    }
}