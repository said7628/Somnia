using System.Threading;
using System.Threading.Tasks;
using Somnia.Economy.Interfaces;

namespace Somnia.Economy.Managers
{
    public class ProgressionEconomyBridge
    {
        private readonly IProgressionGateway _progressionGateway;
        private readonly IPlayerSessionProvider _session;

        public ProgressionEconomyBridge(IProgressionGateway progressionGateway, IPlayerSessionProvider session)
        {
            _progressionGateway = progressionGateway;
            _session = session;
        }

        public Task<bool> IsShopAvailableForIslandAsync(int islandId, CancellationToken ct = default)
        {
            return _progressionGateway.IsIslandUnlockedAsync(_session.CurrentPlayerId, islandId, ct);
        }

        public Task<bool> CanAccessEconomyContentForLevelAsync(int levelId, CancellationToken ct = default)
        {
            return _progressionGateway.IsLevelCompletedAsync(_session.CurrentPlayerId, levelId, ct);
        }
    }
}
