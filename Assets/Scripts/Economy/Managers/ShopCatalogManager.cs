using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Somnia.Economy.DTOs;
using Somnia.Economy.Interfaces;
using Somnia.Economy.Services;

namespace Somnia.Economy.Managers
{
    public class ShopCatalogManager
    {
        private readonly ShopCatalogService _catalogService;
        private readonly IProgressionGateway _progressionGateway;
        private readonly IPlayerSessionProvider _session;

        public ShopCatalogManager(ShopCatalogService catalogService, IProgressionGateway progressionGateway, IPlayerSessionProvider session)
        {
            _catalogService = catalogService;
            _progressionGateway = progressionGateway;
            _session = session;
        }

        public async Task<IReadOnlyList<ShopItemData>> GetAvailableItemsByIslandAsync(int islandId, CancellationToken ct = default)
        {
            var progress = await _progressionGateway.GetPlayerProgressAsync(_session.CurrentPlayerId, ct);
            var completedLevelIds = progress.Where(p => p.completo).Select(p => p.id_nivel).ToArray();
            return await _catalogService.GetItemsForIslandAsync(islandId, completedLevelIds, ct);
        }
    }
}
