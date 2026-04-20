using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

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

        private void Start()
        {
            if (autoStart)
            {
                StartCoroutine(Bootstrap());
            }
        }

        public IEnumerator Bootstrap()
        {
            var resolvedTicket = ResolveTicket();
            if (string.IsNullOrWhiteSpace(resolvedTicket))
            {
                LastError = "No se recibió game ticket";
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
                    if (auth == null || !auth.success || auth.tokens == null)
                    {
                        LastError = "Respuesta inválida en intercambio de ticket";
                        return;
                    }

                    GameSessionManager.Instance.SetSession(auth.user, auth.tokens);
                    IsReady = true;
                },
                onError: (err) => LastError = err,
                withAuth: false
            );
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
    }
}
