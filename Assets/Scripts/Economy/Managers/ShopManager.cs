using System.Threading;
using System.Threading.Tasks;
using Somnia.Economy.DTOs;
using Somnia.Economy.Events;
using Somnia.Economy.Interfaces;

namespace Somnia.Economy.Managers
{
    public class ShopManager
    {
        private readonly CurrencyManager _currencyManager;
        private readonly IShopApiClient _shopApiClient;
        private readonly IInventoryGateway _inventoryGateway;
        private readonly IPlayerSessionProvider _session;
        private readonly ChangeSystemManager _changeSystemManager;
        private readonly EconomyEventBus _eventBus;

        public ShopManager(
            CurrencyManager currencyManager,
            IShopApiClient shopApiClient,
            IInventoryGateway inventoryGateway,
            IPlayerSessionProvider session,
            ChangeSystemManager changeSystemManager,
            EconomyEventBus eventBus)
        {
            _currencyManager = currencyManager;
            _shopApiClient = shopApiClient;
            _inventoryGateway = inventoryGateway;
            _session = session;
            _changeSystemManager = changeSystemManager;
            _eventBus = eventBus;
        }

        public async Task<ApiResponse<PurchaseResponse>> PurchaseAsync(
            ShopItemData item,
            int amountPaid,
            bool useChangeSystem,
            bool? playerSaysHasError = null,
            CancellationToken ct = default)
        {
            _eventBus.Publish(new PurchaseStartedEvent(item.id_item, item.costo));

            var validationFail = await ValidateOwnershipAndFunds(item, ct);
            if (validationFail != null)
            {
                _eventBus.Publish(new PurchaseFailedEvent(item.id_item, validationFail.message));
                return validationFail;
            }

            ChangeChallengeResult changeResult = null;
            if (useChangeSystem)
            {
                var challenge = _changeSystemManager.BuildChallenge(item.costo, amountPaid);

                if (!playerSaysHasError.HasValue)
                {
                    var error = ApiResponse<PurchaseResponse>.Fail(
                        "change_input_missing",
                        "Se requiere respuesta del jugador para resolver el sistema de cambio."
                    );

                    _eventBus.Publish(new PurchaseFailedEvent(item.id_item, error.message));
                    return error;
                }

                changeResult = await _changeSystemManager.ResolveAsync(
                    challenge,
                    playerSaysHasError.Value,
                    ct
                );
            }

            var spendOk = await _currencyManager.SpendYatzisAsync(
                item.costo,
                $"purchase_item_{item.id_item}",
                "shop_purchase",
                ct
            );

            if (!spendOk)
            {
                var failFunds = ApiResponse<PurchaseResponse>.Fail(
                    "insufficient_funds",
                    "No hay Yatzis suficientes."
                );

                _eventBus.Publish(new PurchaseFailedEvent(item.id_item, failFunds.message));
                return failFunds;
            }

            var request = new PurchaseRequest
            {
                id_jugador = _session.CurrentPlayerId,
                id_tienda = item.id_tienda,
                id_tienda_item = item.id_tienda_item,
                cantidad = 1,
                change_challenge_enabled = useChangeSystem,
                change_result = changeResult
            };

            var response = await _shopApiClient.RegisterPurchaseAsync(request, ct);
            if (!response.success || response.data == null)
            {
                await _currencyManager.AddYatzisAsync(
                    item.costo,
                    "purchase_rollback",
                    "shop_purchase",
                    ct
                );

                _eventBus.Publish(new PurchaseFailedEvent(item.id_item, response.message));
                return response;
            }

            await _inventoryGateway.AddItemAsync(_session.CurrentPlayerId, item.id_item, ct);
            await _inventoryGateway.RefreshInventoryAsync(_session.CurrentPlayerId, ct);

            _eventBus.Publish(new PurchaseCompletedEvent(response.data, item.id_item));
            return response;
        }

        private async Task<ApiResponse<PurchaseResponse>> ValidateOwnershipAndFunds(
            ShopItemData item,
            CancellationToken ct)
        {
            if (await _inventoryGateway.HasItemAsync(_session.CurrentPlayerId, item.id_item, ct))
            {
                return ApiResponse<PurchaseResponse>.Fail(
                    "already_owned",
                    "El item ya fue comprado."
                );
            }

            if (!_currencyManager.CanAfford(item.costo))
            {
                return ApiResponse<PurchaseResponse>.Fail(
                    "insufficient_funds",
                    "No hay Yatzis suficientes."
                );
            }

            return null;
        }
    }
}