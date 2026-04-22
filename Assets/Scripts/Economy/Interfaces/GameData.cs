using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Somnia.Economy.DTOs;
using Somnia.UnityClient;

namespace Somnia.Economy.Interfaces
{
    public interface IGameDataApiClient
    {
        Task<ApiResponse<SlotSummary[]>> GetSlotsAsync(CancellationToken ct = default);
        Task<ApiResponse<SlotDetailResponse>> GetSlotDetailAsync(int slotNumber, CancellationToken ct = default);
        Task<ApiResponse<bool>> DeleteSlotAsync(int slotNumber, CancellationToken ct = default);
        Task<ApiResponse<SlotDetailResponse>> CreateSlotAsync(CreateSlotRequest request, CancellationToken ct = default);
        Task<ApiResponse<SlotDetailResponse>> InitializeNewGameAsync(int slotNumber, InitializeGameRequest request, CancellationToken ct = default);
        Task<ApiResponse<List<ProgressData>>> SaveProgressAsync(int slotNumber, SaveProgressRequest request, CancellationToken ct = default);
        Task<ApiResponse<ProgressSaveResult>> SaveProgressEntryAsync(int slotNumber, ProgressData progress, CancellationToken ct = default);
        Task<ApiResponse<EquippedItemsData>> UpdateEquipmentAsync(int slotNumber, UpdateEquipmentRequest request, CancellationToken ct = default);
    }

    public interface IGameDataService
    {
        Task<ApiResponse<SlotSummary[]>> GetSlotsAsync(CancellationToken ct = default);
        Task<ApiResponse<SlotDetailResponse>> GetSlotDetailAsync(int slotNumber, CancellationToken ct = default);
        Task<ApiResponse<bool>> DeleteSlotAsync(int slotNumber, CancellationToken ct = default);
        Task<ApiResponse<SlotDetailResponse>> CreateSlotAsync(int slotNumber, string slotName, CancellationToken ct = default);
        Task<ApiResponse<SlotDetailResponse>> InitializeNewGameAsync(int slotNumber, string slotName, CancellationToken ct = default);
        Task<ApiResponse<EconomyBalanceResponse>> GetBalanceAsync(CancellationToken ct = default);
        Task<ApiResponse<EconomyBalanceResponse>> SaveYatzisAsync(int targetYatzis, string reason, CancellationToken ct = default);
        Task<ApiResponse<IReadOnlyList<ShopItemViewData>>> GetShopItemsAsync(int islandId, int slotNumber, CancellationToken ct = default);
        Task<ApiResponse<PurchaseResponse>> PurchaseItemAsync(ShopItemData item, int amountPaid, bool useChangeSystem, bool? playerSaysHasError, CancellationToken ct = default);
        Task<ApiResponse<IReadOnlyList<InventarioItemDto>>> GetInventoryAsync(int slotNumber, CancellationToken ct = default);
        Task<ApiResponse<EquippedItemsData>> GetEquippedItemsAsync(int slotNumber, CancellationToken ct = default);
        Task<ApiResponse<EquippedItemsData>> UpdateEquipmentAsync(int slotNumber, UpdateEquipmentRequest request, CancellationToken ct = default);
        Task<ApiResponse<IReadOnlyList<ProgressData>>> LoadProgressAsync(int slotNumber, CancellationToken ct = default);
        Task<ApiResponse<IReadOnlyList<ProgressData>>> SaveProgressAsync(int slotNumber, IReadOnlyList<ProgressData> progress, CancellationToken ct = default);
    }
}