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

        
        [SerializeField] private bool enableHttpDebugLogs = true;

        private void Awake()
        {
            if (apiConfig == null)
            {
                apiConfig = ScriptableObject.CreateInstance<ApiConfig>();
            }

            if (enableHttpDebugLogs)
            {
                Debug.Log($"[GameApiClient] Base URL configurada: {apiConfig.ApiBaseUrl}");
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
            var fullUrl = apiConfig.BuildUrl(path);
            var request = new UnityWebRequest(fullUrl, UnityWebRequest.kHttpVerbPOST);
            var bodyRaw = Encoding.UTF8.GetBytes(jsonBody ?? "{}");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            if (withAuth && GameSessionManager.Instance != null && GameSessionManager.Instance.HasSession())
            {
                request.SetRequestHeader("Authorization", $"Bearer {GameSessionManager.Instance.AccessToken}");
            }
            request.timeout = apiConfig.TimeoutSeconds;

            if (enableHttpDebugLogs)
            {
                Debug.Log($"[HTTP] POST {fullUrl} | Request: {jsonBody}");
            }

            yield return request.SendWebRequest();

            if (enableHttpDebugLogs)
            {
                Debug.Log($"[HTTP] POST {fullUrl} -> {(int)request.responseCode} | Body: {request.downloadHandler.text}");
            }

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
            var fullUrl = apiConfig.BuildUrl(path);
            using var request = UnityWebRequest.Get(fullUrl);
            request.SetRequestHeader("Content-Type", "application/json");
            if (withAuth && GameSessionManager.Instance != null && GameSessionManager.Instance.HasSession())
            {
                request.SetRequestHeader("Authorization", $"Bearer {GameSessionManager.Instance.AccessToken}");
            }
            request.timeout = apiConfig.TimeoutSeconds;

            if (enableHttpDebugLogs)
            {
                Debug.Log($"[HTTP] GET {fullUrl}");
            }

            yield return request.SendWebRequest();

            if (enableHttpDebugLogs)
            {
                Debug.Log($"[HTTP] GET {fullUrl} -> {(int)request.responseCode} | Body: {request.downloadHandler.text}");
            }

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