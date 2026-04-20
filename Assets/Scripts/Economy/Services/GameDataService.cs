using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Somnia.Economy.DTOs;
using Somnia.Economy.Interfaces;
using Somnia.Economy.Managers;
using Somnia.UnityClient;

namespace Somnia.Economy.Services
{
 
    /// flujo de datos de juego/eco

    public class GameDataService : IGameDataService
    {
        private readonly IGameDataApiClient _gameDataApiClient;
        private readonly PlayerEconomyService _economyService;
        private readonly ShopCatalogService _catalogService;
        private readonly ShopManager _shopManager;

        public GameDataService(
            IGameDataApiClient gameDataApiClient,
            PlayerEconomyService economyService,
            ShopCatalogService catalogService,
            ShopManager shopManager)
        {
            _gameDataApiClient = gameDataApiClient;
            _economyService = economyService;
            _catalogService = catalogService;
            _shopManager = shopManager;
        }

        public Task<ApiResponse<SlotSummary[]>> GetSlotsAsync(CancellationToken ct = default) => _gameDataApiClient.GetSlotsAsync(ct);

        public Task<ApiResponse<SlotDetailResponse>> GetSlotDetailAsync(int slotNumber, CancellationToken ct = default) =>
            _gameDataApiClient.GetSlotDetailAsync(slotNumber, ct);

        public Task<ApiResponse<SlotDetailResponse>> CreateSlotAsync(int slotNumber, string slotName, CancellationToken ct = default)
        {
            return _gameDataApiClient.CreateSlotAsync(new CreateSlotRequest
            {
                slot_numero = slotNumber,
                nombre_slot = slotName
            }, ct);
        }

        public async Task<ApiResponse<SlotDetailResponse>> InitializeNewGameAsync(int slotNumber, string slotName, CancellationToken ct = default)
        {
            var createResult = await CreateSlotAsync(slotNumber, slotName, ct);
            if (!createResult.success)
            {
                return createResult;
            }

            var initResult = await _gameDataApiClient.InitializeNewGameAsync(slotNumber, new InitializeGameRequest
            {
                force_reset_yatzis = true
            }, ct);

            if (!initResult.success)
            {
                return initResult;
            }

            var slotBalance = initResult.data?.slot?.yatzis ?? 0;
            if (slotBalance != 0)
            {
                var delta = -slotBalance;
                await _economyService.UpdateBalanceAsync(delta, "new_game_reset", "game_initializer", ct);
            }

            return await GetSlotDetailAsync(slotNumber, ct);
        }

        public Task<ApiResponse<EconomyBalanceResponse>> GetBalanceAsync(CancellationToken ct = default) => _economyService.GetBalanceAsync(ct);

        public async Task<ApiResponse<EconomyBalanceResponse>> SaveYatzisAsync(int targetYatzis, string reason, CancellationToken ct = default)
        {
            var current = await GetBalanceAsync(ct);
            if (!current.success || current.data == null)
            {
                return ApiResponse<EconomyBalanceResponse>.Fail("balance_unavailable", current.message ?? "No se pudo leer balance actual");
            }

            var delta = targetYatzis - current.data.yatzis;
            if (delta == 0)
            {
                return current;
            }

            return await _economyService.UpdateBalanceAsync(delta, reason, "game_data_service", ct);
        }

        public async Task<ApiResponse<IReadOnlyList<ShopItemViewData>>> GetShopItemsAsync(int islandId, int slotNumber, CancellationToken ct = default)
        {
            var catalog = await _catalogService.GetItemsForIslandAsync(islandId, ct: ct);
            var detail = await GetSlotDetailAsync(slotNumber, ct);
            if (!detail.success || detail.data == null)
            {
                return ApiResponse<IReadOnlyList<ShopItemViewData>>.Fail("slot_detail_unavailable", detail.message ?? "No fue posible leer inventario/equipamiento");
            }

            var inventoryIds = new HashSet<int>((detail.data.inventario_cosmeticos ?? new InventarioItemDto[0]).Select(x => x.id_item));
            var equipped = MapEquipped(detail.data.equipamiento);

            var mapped = catalog.Select(item => new ShopItemViewData
            {
                id = item.id_item,
                nombre = item.nombre,
                costo = item.costo,
                tipo = item.tipo,
                descripcion = item.descripcion,
                owned = inventoryIds.Contains(item.id_item),
                equipped = equipped.IsEquipped(item.id_item),
                id_tienda = item.id_tienda,
                id_tienda_item = item.id_tienda_item
            }).ToList();

            return ApiResponse<IReadOnlyList<ShopItemViewData>>.Ok(mapped);
        }

        public Task<ApiResponse<PurchaseResponse>> PurchaseItemAsync(ShopItemData item, int amountPaid, bool useChangeSystem, bool? playerSaysHasError, CancellationToken ct = default)
            => _shopManager.PurchaseAsync(item, amountPaid, useChangeSystem, playerSaysHasError, ct);

        public async Task<ApiResponse<IReadOnlyList<InventarioItemDto>>> GetInventoryAsync(int slotNumber, CancellationToken ct = default)
        {
            var detail = await GetSlotDetailAsync(slotNumber, ct);
            if (!detail.success || detail.data == null)
            {
                return ApiResponse<IReadOnlyList<InventarioItemDto>>.Fail("slot_detail_unavailable", detail.message ?? "No se pudo obtener inventario");
            }

            return ApiResponse<IReadOnlyList<InventarioItemDto>>.Ok(detail.data.inventario_cosmeticos ?? new InventarioItemDto[0]);
        }

        public async Task<ApiResponse<EquippedItemsData>> GetEquippedItemsAsync(int slotNumber, CancellationToken ct = default)
        {
            var detail = await GetSlotDetailAsync(slotNumber, ct);
            if (!detail.success || detail.data == null)
            {
                return ApiResponse<EquippedItemsData>.Fail("slot_detail_unavailable", detail.message ?? "No se pudo obtener equipamiento");
            }

            return ApiResponse<EquippedItemsData>.Ok(MapEquipped(detail.data.equipamiento));
        }

        public Task<ApiResponse<EquippedItemsData>> UpdateEquipmentAsync(int slotNumber, UpdateEquipmentRequest request, CancellationToken ct = default)
            => _gameDataApiClient.UpdateEquipmentAsync(slotNumber, request, ct);

        public async Task<ApiResponse<IReadOnlyList<ProgressData>>> LoadProgressAsync(int slotNumber, CancellationToken ct = default)
        {
            var detail = await GetSlotDetailAsync(slotNumber, ct);
            if (!detail.success || detail.data == null)
            {
                return ApiResponse<IReadOnlyList<ProgressData>>.Fail("slot_detail_unavailable", detail.message ?? "No se pudo obtener progreso");
            }

            var mapped = (detail.data.progreso ?? new ProgresoDto[0]).Select(p => new ProgressData
            {
                id_nivel = p.id_nivel,
                completo = p.completo == 1,
                puntuacion_maxima = p.puntuacion_maxima
            }).ToList();

            return ApiResponse<IReadOnlyList<ProgressData>>.Ok(mapped);
        }

        public async Task<ApiResponse<IReadOnlyList<ProgressData>>> SaveProgressAsync(int slotNumber, IReadOnlyList<ProgressData> progress, CancellationToken ct = default)
        {
            var response = await _gameDataApiClient.SaveProgressAsync(slotNumber, new SaveProgressRequest
            {
                progreso = progress?.ToList() ?? new List<ProgressData>()
            }, ct);

            return response.success
                ? ApiResponse<IReadOnlyList<ProgressData>>.Ok(response.data ?? new List<ProgressData>(), response.message)
                : ApiResponse<IReadOnlyList<ProgressData>>.Fail(response.error?.code ?? "save_progress_error", response.message);
        }

        private static EquippedItemsData MapEquipped(EquipamientoDto dto)
        {
            return new EquippedItemsData
            {
                id_item_cara = dto?.id_item_cara ?? 0,
                id_item_color = dto?.id_item_color ?? 0,
                id_item_outfit = dto?.id_item_outfit ?? 0
            };
        }
    }
}
