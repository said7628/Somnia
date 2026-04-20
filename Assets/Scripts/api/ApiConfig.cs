using UnityEngine;

namespace Somnia.UnityClient
{
    [CreateAssetMenu(fileName = "ApiConfig", menuName = "Somnia/API Config")]
    public class ApiConfig : ScriptableObject
    {
        [Header("Base URL del backend lambda")]
        public const string DefaultApiBaseUrl = "https://nwbbsidozdpfkxpylna7pdg7qu0renci.lambda-url.us-east-1.on.aws/";
        public string ApiBaseUrl = DefaultApiBaseUrl;

        [Header("Ruta para intercambio de ticket")]
        public string TicketExchangePath = "/game/auth/exchange";

        [Header("Timeout en segundos")]
        public int TimeoutSeconds = 20;

        public string BuildUrl(string path)
        {
            var baseUrl = (ApiBaseUrl ?? string.Empty).TrimEnd('/');
            var cleanPath = (path ?? string.Empty).TrimStart('/');
            return $"{baseUrl}/{cleanPath}";
        }
    }
}