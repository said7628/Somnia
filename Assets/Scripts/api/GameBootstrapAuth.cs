using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace Somnia.UnityClient
{
    public class GameBootstrapAuth : MonoBehaviour
    {
        [SerializeField] private GameApiClient apiClient;

        [Header("Si se define manualmente, tiene prioridad sobre URL")]
        [SerializeField] private string gameTicket;

        public bool IsReady { get; private set; }
        public string LastError { get; private set; }

        [SerializeField] private bool autoStart = true;
        [SerializeField] private bool loadFailSceneOnError = true;
        [SerializeField] private string failSceneName = "Fail";
        private bool failSceneTriggered;

        private void Start()
        {
            if (autoStart)
            {
                StartCoroutine(Bootstrap());
            }
        }

        public IEnumerator Bootstrap()
        {
            IsReady = false;
            LastError = null;
            failSceneTriggered = false;

            if (apiClient == null)
            {
                Fail("No hay GameApiClient configurado");
                yield break;
            }

            var resolvedTicket = ResolveTicket();
            if (string.IsNullOrWhiteSpace(resolvedTicket))
            {
                Fail("No se recibió game ticket");
                yield break;
            }

            var request = new ExchangeTicketRequest { ticket = resolvedTicket };
            var requestJson = JsonUtility.ToJson(request);

            yield return apiClient.PostJson(
                apiClient.ConfiguredTicketExchangePath(),
                requestJson,
                onSuccess: (raw) =>
                {
                    var auth = JsonUtility.FromJson<GameAuthResponse>(raw);
                    if (auth == null)
                    {
                        Fail("Respuesta inválida en intercambio de ticket");
                        return;
                    }

                    if (!auth.success)
                    {
                        Fail(string.IsNullOrWhiteSpace(auth.message)
                            ? "Intercambio de ticket rechazado"
                            : auth.message);
                        return;
                    }

                    if (auth.user == null || auth.tokens == null)
                    {
                        Fail("Respuesta inválida en intercambio de ticket");
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(auth.tokens.accessToken) || string.IsNullOrWhiteSpace(auth.tokens.refreshToken))
                    {
                       
                        Fail("Tokens inválidos en respuesta de autenticación");
                        return;
                    }

                    GameSessionManager.Instance.SetSession(auth.user, auth.tokens);
                    IsReady = true;
                },
                onError: (err) => Fail(string.IsNullOrWhiteSpace(err) ? "Falló la llamada de intercambio de ticket" : err),
                withAuth: false
            );
        }

        private void Fail(string error)
        {
            LastError = error;
            IsReady = false;

            if (!loadFailSceneOnError || failSceneTriggered) return;

            failSceneTriggered = true;
            if (Application.CanStreamedLevelBeLoaded(failSceneName))
            {
                SceneManager.LoadScene(failSceneName);
                return;
            }

            Debug.LogError($"GameBootstrapAuth: no se puede cargar la escena '{failSceneName}' porque no está en Build Settings.");
        }

        private string ResolveTicket()
        {
            if (!string.IsNullOrWhiteSpace(gameTicket)) return gameTicket;
            if (Application.platform != RuntimePlatform.WebGLPlayer) return string.Empty;

            var url = Application.absoluteURL;
            if (string.IsNullOrWhiteSpace(url) || !url.Contains("game_ticket=")) return string.Empty;

            var idx = url.IndexOf("game_ticket=") + "game_ticket=".Length;
            var end = url.IndexOf('&', idx);
            var value = end >= 0 ? url.Substring(idx, end - idx) : url.Substring(idx);
            return UnityWebRequest.UnEscapeURL(value);
        }

        public void SetTicket(string ticket)
        {
            gameTicket = ticket;
        }

        public void SetApiClient(GameApiClient client)
        {
            apiClient = client;
        }

        public void SetAutoStart(bool enabled)
        {
            autoStart = enabled;
        }

        public void SetFailScene(string sceneName)
        {
            failSceneName = sceneName;
        }
    }
}
