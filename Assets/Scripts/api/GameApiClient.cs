using System;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Somnia.UnityClient
{
    public class GameApiClient : MonoBehaviour
    {
        [SerializeField] private ApiConfig apiConfig;

        private void Awake()
        {
            if (apiConfig == null)
            {
                apiConfig = ScriptableObject.CreateInstance<ApiConfig>();
            }
        }

        public void SetConfig(ApiConfig config)
        {
            apiConfig = config;
        }

        public string ConfiguredTicketExchangePath()
        {
            return apiConfig != null && !string.IsNullOrWhiteSpace(apiConfig.TicketExchangePath)
                ? apiConfig.TicketExchangePath
                : "/game/auth/exchange";
        }

        public IEnumerator PostJson(string path, string jsonBody, Action<string> onSuccess, Action<string> onError, bool withAuth = false)
        {
            var request = new UnityWebRequest(apiConfig.BuildUrl(path), UnityWebRequest.kHttpVerbPOST);
            var bodyRaw = Encoding.UTF8.GetBytes(jsonBody ?? "{}");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            if (withAuth && GameSessionManager.Instance != null && GameSessionManager.Instance.HasSession())
            {
                request.SetRequestHeader("Authorization", $"Bearer {GameSessionManager.Instance.AccessToken}");
            }
            request.timeout = apiConfig.TimeoutSeconds;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(request.downloadHandler.text);
            }
            else
            {
                onError?.Invoke($"{request.responseCode}: {request.downloadHandler.text}");
            }
        }

        public IEnumerator GetJson(string path, Action<string> onSuccess, Action<string> onError, bool withAuth = true)
        {
            using var request = UnityWebRequest.Get(apiConfig.BuildUrl(path));
            request.SetRequestHeader("Content-Type", "application/json");
            if (withAuth && GameSessionManager.Instance != null && GameSessionManager.Instance.HasSession())
            {
                request.SetRequestHeader("Authorization", $"Bearer {GameSessionManager.Instance.AccessToken}");
            }
            request.timeout = apiConfig.TimeoutSeconds;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(request.downloadHandler.text);
            }
            else
            {
                onError?.Invoke($"{request.responseCode}: {request.downloadHandler.text}");
            }
        }
    }
}
