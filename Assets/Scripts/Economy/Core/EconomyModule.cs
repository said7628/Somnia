using Somnia.Economy.Config;
using Somnia.Economy.Events;
using Somnia.Economy.Interfaces;
using Somnia.Economy.Managers;
using Somnia.Economy.Services;
using Somnia.Economy.Services.ApiClients;
using Somnia.Economy.Services.Mocks;
using UnityEngine;

namespace Somnia.Economy.Core
{
    
    /// modulo económico y capa de datos centralizada.
    

    public class EconomyModule : MonoBehaviour
    {
        [SerializeField] private EconomyConfig config;
        [SerializeField] private string apiBaseUrl = "https://api.somnia-game.com";
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

        private async void Awake()
        {
            EventBus = new EconomyEventBus();

            IPlayerSessionProvider session = useMockGateways
                ? new MockPlayerSessionProvider()
                : new GameSessionPlayerProvider();

            var economyApiClient = new PlayerEconomyApiClient(apiBaseUrl, session);
            var shopApiClient = new ShopApiClient(apiBaseUrl, session);
            var gameDataApiClient = new GameDataApiClient(apiBaseUrl, session);

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
        }
    }
}
