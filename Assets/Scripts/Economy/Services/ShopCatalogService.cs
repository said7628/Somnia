using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Somnia.Economy.DTOs;
using Somnia.Economy.Interfaces;

namespace Somnia.Economy.Services
{
    public class ShopCatalogService
    {
        private readonly IShopApiClient _shopApiClient;
        private readonly IPlayerSessionProvider _session;

        public ShopCatalogService(IShopApiClient shopApiClient, IPlayerSessionProvider session)
        {
            _shopApiClient = shopApiClient;
            _session = session;
        }

        public async Task<IReadOnlyList<ShopItemData>> GetItemsForIslandAsync(int islandId, IReadOnlyCollection<int> completedLevels = null, CancellationToken ct = default)
        {
            var response = await _shopApiClient.GetCatalogByIslandAsync(_session.CurrentPlayerId, islandId, ct);
            if (!response.success || response.data == null)
            {
                return new List<ShopItemData>();
            }

            var items = response.data.items ?? new List<ShopItemData>();
            if (completedLevels == null || completedLevels.Count == 0)
            {
                return items;
            }

            return items.Where(item => item.required_level_id <= 0 || completedLevels.Contains(item.required_level_id)).ToList();
        }
    }
}
