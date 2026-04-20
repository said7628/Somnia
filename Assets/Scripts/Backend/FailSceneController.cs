using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Somnia.UnityClient
{
    public class FailSceneController : MonoBehaviour
    {
        [Header("Escena a la que regresara el boton")]
        [SerializeField] private string loadingSceneName = "Loading";

        [Header("Boton ReturnMenu")]
        [SerializeField] private Button returnMenuButton;

        private void Awake()
        {
            if (returnMenuButton != null)
            {
                returnMenuButton.onClick.RemoveAllListeners();
                returnMenuButton.onClick.AddListener(ReturnToLoading);
            }
        }

        private void ReturnToLoading()
        {
            SceneManager.LoadScene(loadingSceneName);
        }
    }
}