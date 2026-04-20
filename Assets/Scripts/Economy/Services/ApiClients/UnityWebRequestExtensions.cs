using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Somnia.Economy.Services.ApiClients
{
    internal static class UnityWebRequestExtensions
    {
        public static async Task<string> SendAsync(this UnityWebRequest request, CancellationToken ct)
        {
            using var registration = ct.Register(request.Abort);
            var op = request.SendWebRequest();
            while (!op.isDone)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Yield();
            }

            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                throw new InvalidOperationException($"HTTP {(int)request.responseCode}: {request.error} - {request.downloadHandler?.text}");
            }

            return request.downloadHandler?.text ?? string.Empty;
        }

        public static UnityWebRequest CreateJsonRequest(string url, string method, string jsonBody, string bearerToken)
        {
            var req = new UnityWebRequest(url, method)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody ?? "{}")),
                downloadHandler = new DownloadHandlerBuffer()
            };
            req.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrWhiteSpace(bearerToken))
            {
                req.SetRequestHeader("Authorization", $"Bearer {bearerToken}");
            }
            return req;
        }

        public static UnityWebRequest CreateGet(string url, string bearerToken)
        {
            var req = UnityWebRequest.Get(url);
            req.downloadHandler = new DownloadHandlerBuffer();
            if (!string.IsNullOrWhiteSpace(bearerToken))
            {
                req.SetRequestHeader("Authorization", $"Bearer {bearerToken}");
            }
            return req;
        }

        public static T ParseJson<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return default;
            }
            return JsonUtility.FromJson<T>(json);
        }
    }
}
