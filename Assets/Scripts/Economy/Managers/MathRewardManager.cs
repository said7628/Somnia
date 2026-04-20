using System;
using System.Threading;
using System.Threading.Tasks;
using Somnia.Economy.Config;
using Somnia.Economy.Events;
using Somnia.Economy.Services;

namespace Somnia.Economy.Managers
{
    public class MathRewardManager
    {
        private readonly EconomyConfig _config;
        private readonly CurrencyManager _currencyManager;
        private readonly PlayerEconomyService _playerEconomyService;
        private readonly EconomyEventBus _eventBus;

        public MathRewardManager(
            EconomyConfig config,
            CurrencyManager currencyManager,
            PlayerEconomyService playerEconomyService,
            EconomyEventBus eventBus)
        {
            _config = config;
            _currencyManager = currencyManager;
            _playerEconomyService = playerEconomyService;
            _eventBus = eventBus;
        }

        public async Task<int> GrantRewardFromScoreAsync(int score, int islandId, int levelId, CancellationToken ct = default)
        {
            score = Math.Max(0, score);

            var ageDataResponse = await _playerEconomyService.GetRangeAgeAsync(ct);
            var ageMultiplier = ageDataResponse.success && ageDataResponse.data != null
                ? ageDataResponse.data.multiplicador
                : _config.minAgeMultiplier;

            var islandMultiplier = _config.GetIslandBonus(islandId);
            var yatzis = (int)Math.Round(score * _config.scoreToYatzisFactor * ageMultiplier * islandMultiplier);
            yatzis = Math.Max(0, yatzis);

            if (yatzis <= 0) return 0;

            var granted = await _currencyManager.AddYatzisAsync(yatzis, $"math_level_{levelId}", "math_reward", ct);
            if (granted)
            {
                _eventBus.Publish(new RewardGrantedEvent("math", yatzis, levelId));
                return yatzis;
            }

            return 0;
        }
    }
}
