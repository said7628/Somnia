using System.Threading;
using System.Threading.Tasks;
using Somnia.Economy.DTOs;

namespace Somnia.Economy.Interfaces
{
    public interface IShopApiClient
    {
        Task<ApiResponse<ShopCatalogResponse>> GetCatalogByIslandAsync(int playerId, int islandId, CancellationToken ct = default);
        Task<ApiResponse<PurchaseResponse>> RegisterPurchaseAsync(PurchaseRequest request, CancellationToken ct = default);
    }

    public interface IPlayerEconomyApiClient
    {
        Task<ApiResponse<PlayerEconomyData>> GetPlayerEconomyAsync(int playerId, CancellationToken ct = default);
        Task<ApiResponse<EconomyBalanceResponse>> GetBalanceAsync(int playerId, CancellationToken ct = default);
        Task<ApiResponse<EconomyBalanceResponse>> UpdateBalanceAsync(UpdateBalanceRequest request, CancellationToken ct = default);
        Task<ApiResponse<RangeAgeData>> GetRangeAgeAsync(int playerId, CancellationToken ct = default);
    }
}
