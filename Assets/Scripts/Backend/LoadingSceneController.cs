using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Somnia.UnityClient
{
    public class LoadingSceneController : MonoBehaviour
    {
        [Header("Escenas")]
        [SerializeField] private string successSceneName = "Pantalla_principal";
        [SerializeField] private string failSceneName = "Fail";

        [Header("Ticket manual para pruebas locales (opcional)")]
        [SerializeField] private string gameTicket = string.Empty;

        private bool isRunning;
        private bool sceneTransitionTriggered;

        private void Start()
        {
            if (!isRunning)
            {
                StartCoroutine(RunLoadingFlow());
            }
        }

        private IEnumerator RunLoadingFlow()
        {
            isRunning = true;

            var services = BackendServices.GetOrCreate();
            services.ClearBootstrapError();

            var session = GameSessionManager.Instance;

            if (!string.IsNullOrWhiteSpace(gameTicket))
            {
                services.BootstrapAuth.SetTicket(gameTicket);
            }

            yield return services.BootstrapAuth.Bootstrap();

            if (!string.IsNullOrEmpty(services.BootstrapAuth.LastError))
            {
                services.SetBootstrapError(services.BootstrapAuth.LastError);
                LoadSceneOnce(failSceneName);
                yield break;
            }

            session = GameSessionManager.Instance;

            if (session == null)
            {
                services.SetBootstrapError("No existe GameSessionManager.");
                LoadSceneOnce(failSceneName);
                yield break;
            }

            if (!session.HasSession())
            {
                services.SetBootstrapError("No se pudo crear una sesion valida.");
                LoadSceneOnce(failSceneName);
                yield break;
            }

            LoadSceneOnce(successSceneName);
        }

        private void LoadSceneOnce(string sceneName)
        {
            if (sceneTransitionTriggered)
            {
                return;
            }

            sceneTransitionTriggered = true;
            SceneManager.LoadScene(sceneName);
        }
    }
}