using UnityEngine;

namespace Somnia.UnityClient
{
    public class BackendServices : MonoBehaviour
    {
        public static BackendServices Instance { get; private set; }

        public GameApiClient ApiClient { get; private set; }
        public GameBootstrapAuth BootstrapAuth { get; private set; }
        public SlotsLoader SlotsLoader { get; private set; }
        public SlotDetailLoader SlotDetailLoader { get; private set; }

        public string LastBootstrapError { get; private set; } = string.Empty;

        public static BackendServices GetOrCreate()
        {
            if (Instance != null)
            {
                return Instance;
            }

            var go = new GameObject("[BackendServices]");
            Instance = go.AddComponent<BackendServices>();
            Instance.Initialize();
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Initialize()
        {
            DontDestroyOnLoad(gameObject);

            if (GameSessionManager.Instance == null)
            {
                var sessionGo = new GameObject("[GameSessionManager]");
                sessionGo.AddComponent<GameSessionManager>();
                DontDestroyOnLoad(sessionGo);
            }

            var runtimeConfig = ScriptableObject.CreateInstance<ApiConfig>();
            runtimeConfig.name = "RuntimeApiConfig";

            ApiClient = gameObject.AddComponent<GameApiClient>();
            ApiClient.SetConfig(runtimeConfig);

            BootstrapAuth = gameObject.AddComponent<GameBootstrapAuth>();
            BootstrapAuth.SetApiClient(ApiClient);
            BootstrapAuth.SetAutoStart(false);
            BootstrapAuth.SetLoadFailSceneOnError(false);

            SlotsLoader = gameObject.AddComponent<SlotsLoader>();
            SlotsLoader.SetApiClient(ApiClient);

            SlotDetailLoader = gameObject.AddComponent<SlotDetailLoader>();
            SlotDetailLoader.SetApiClient(ApiClient);
        }

        public void SetBootstrapError(string error)
        {
            LastBootstrapError = error ?? string.Empty;
        }

        public void ClearBootstrapError()
        {
            LastBootstrapError = string.Empty;
        }
    }
}