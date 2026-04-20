using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Somnia.Economy.Config;
using Somnia.Economy.Events;
using Somnia.Economy.Interfaces;

namespace Somnia.Economy.Managers
{
    public class PlatformRewardManager
    {
        private readonly EconomyConfig _config;
        private readonly CurrencyManager _currencyManager;
        private readonly IProgressionGateway _progressionGateway;
        private readonly IPlayerSessionProvider _session;
        private readonly EconomyEventBus _eventBus;
        private readonly HashSet<int> _locallyGrantedLevels = new();

        public PlatformRewardManager(EconomyConfig config,
            CurrencyManager currencyManager,
            IProgressionGateway progressionGateway,
            IPlayerSessionProvider session,
            EconomyEventBus eventBus)
        {
            _config = config;
            _currencyManager = currencyManager;
            _progressionGateway = progressionGateway;
            _session = session;
            _eventBus = eventBus;
        }

        public async Task<bool> TryGrantCompletionRewardAsync(int levelId, int score, CancellationToken ct = default)
        {
            if (_locallyGrantedLevels.Contains(levelId)) return false;

            var playerId = _session.CurrentPlayerId;
            if (await _progressionGateway.IsLevelCompletedAsync(playerId, levelId, ct))
            {
                return false;
            }

            var reward = _config.GetPlatformRewardOrDefault(levelId);
            var added = await _currencyManager.AddYatzisAsync(reward, $"platform_level_{levelId}", "platform_reward", ct);
            if (!added) return false;

            await _progressionGateway.MarkLevelCompletedAsync(playerId, levelId, score, ct);
            _locallyGrantedLevels.Add(levelId);
            _eventBus.Publish(new RewardGrantedEvent("platform", reward, levelId));
            return true;
        }
    }
}
