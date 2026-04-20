using Somnia.Economy.Config;
using Somnia.Economy.Events;
using Somnia.Economy.Interfaces;
using Somnia.Economy.Managers;
using Somnia.Economy.Services;
using Somnia.Economy.Services.ApiClients;
using Somnia.Economy.Services.Mocks;
using Somnia.UnityClient;
using UnityEngine;

namespace Somnia.Economy.Core
{
    public class EconomyModule : MonoBehaviour
    {
        public static EconomyModule Instance { get; private set; }

        [SerializeField] private EconomyConfig config;
        [SerializeField] private string apiBaseUrl = ApiConfig.DefaultApiBaseUrl;
        [SerializeField] private bool useMockGateways;

        public EconomyEventBus EventBus { get; private set; }
        public CurrencyManager CurrencyManager { get; private set; }
        public MathRewardManager MathRewardManager { get; private set; }
        public PlatformRewardManager PlatformRewardManager { get; private set; }
        public ChangeSystemManager ChangeSystemManager { get; private set; }
        public ShopCatalogManager ShopCatalogManager { get; private set; }
        public ShopManager ShopManager { get; private set; }
        public ProgressionEconomyBridge ProgressionBridge { get; private set; }
        public IGameDataService GameDataService { get; private set; }

        public bool IsInitialized { get; private set; }

        private async void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (config == null)
            {
                config = ScriptableObject.CreateInstance<EconomyConfig>();
            }

            var resolvedBaseUrl = ResolveBaseUrl();
            Debug.Log($"[EconomyModule] Base URL para servicios de juego/economia: {resolvedBaseUrl}");

            EventBus = new EconomyEventBus();

            IPlayerSessionProvider session = useMockGateways
                ? new MockPlayerSessionProvider()
                : new GameSessionPlayerProvider();

            var economyApiClient = new PlayerEconomyApiClient(resolvedBaseUrl, session);
            var shopApiClient = new ShopApiClient(resolvedBaseUrl, session);
            var gameDataApiClient = new GameDataApiClient(resolvedBaseUrl, session);

            var economyService = new PlayerEconomyService(economyApiClient, session);
            var catalogService = new ShopCatalogService(shopApiClient, session);

            CurrencyManager = new CurrencyManager(economyService, EventBus);
            ChangeSystemManager = new ChangeSystemManager(config, CurrencyManager, EventBus);

            IInventoryGateway inventory = useMockGateways
                ? new MockInventoryGateway()
                : new BackendInventoryGateway(() => GameDataService, session);

            IProgressionGateway progression = useMockGateways
                ? new MockProgressionGateway()
                : new BackendProgressionGateway(() => GameDataService, session);

            MathRewardManager = new MathRewardManager(config, CurrencyManager, economyService, EventBus);
            PlatformRewardManager = new PlatformRewardManager(config, CurrencyManager, progression, session, EventBus);
            ShopCatalogManager = new ShopCatalogManager(catalogService, progression, session);
            ProgressionBridge = new ProgressionEconomyBridge(progression, session);
            ShopManager = new ShopManager(CurrencyManager, shopApiClient, inventory, session, ChangeSystemManager, EventBus);

            GameDataService = new GameDataService(gameDataApiClient, economyService, catalogService, ShopManager);

            await CurrencyManager.InitializeAsync();
            IsInitialized = true;
        }

        private string ResolveBaseUrl()
        {
            if (!string.IsNullOrWhiteSpace(apiBaseUrl))
            {
                return apiBaseUrl;
            }

            return ApiConfig.DefaultApiBaseUrl;
        }
    }
}
