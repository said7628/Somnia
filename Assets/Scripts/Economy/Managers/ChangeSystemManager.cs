using System;
using System.Threading;
using System.Threading.Tasks;
using Somnia.Economy.Config;
using Somnia.Economy.DTOs;
using Somnia.Economy.Events;
using UnityEngine;

namespace Somnia.Economy.Managers
{
    public class ChangeSystemManager
    {
        private readonly EconomyConfig _config;
        private readonly CurrencyManager _currencyManager;
        private readonly EconomyEventBus _eventBus;
        private readonly System.Random _random;

        public ChangeSystemManager(EconomyConfig config, CurrencyManager currencyManager, EconomyEventBus eventBus, int? seed = null)
        {
            _config = config;
            _currencyManager = currencyManager;
            _eventBus = eventBus;
            _random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        }

        public ChangeChallengeData BuildChallenge(int cost, int amountPaid)
        {
            var realChange = Mathf.Max(0, amountPaid - cost);
            var shouldBeWrong = _random.NextDouble() <= _config.wrongChangeProbability;

            var challenge = new ChangeChallengeData
            {
                purchase_cost = cost,
                amount_paid = amountPaid,
                real_change = realChange,
                has_error = shouldBeWrong,
                displayed_change = realChange,
                error_percentage = 0
            };

            if (shouldBeWrong && realChange > 0)
            {
                var percent = _config.wrongChangeMinPercent +
                              ((float)_random.NextDouble() * (_config.wrongChangeMaxPercent - _config.wrongChangeMinPercent));
                var delta = Mathf.Max(1, Mathf.RoundToInt(realChange * percent));
                var sign = _random.Next(0, 2) == 0 ? -1 : 1;
                challenge.displayed_change = Mathf.Max(0, realChange + sign * delta);
                challenge.error_percentage = Mathf.RoundToInt(percent * 100f);
            }

            _eventBus.Publish(new ChangeChallengeStartedEvent(challenge));
            return challenge;
        }

        public async Task<ChangeChallengeResult> ResolveAsync(ChangeChallengeData challenge, bool playerSaysHasError, CancellationToken ct = default)
        {
            var correct = challenge.has_error == playerSaysHasError;
            var delta = correct ? _config.changeSuccessReward : -_config.changeFailurePenalty;

            if (delta >= 0)
            {
                await _currencyManager.AddYatzisAsync(delta, "change_system_result", "change_system", ct);
            }
            else
            {
                var penalty = Math.Min(_currencyManager.Balance, Math.Abs(delta));
                if (penalty > 0)
                {
                    await _currencyManager.SpendYatzisAsync(penalty, "change_system_penalty", "change_system", ct);
                }
                delta = -penalty;
            }

            var result = new ChangeChallengeResult
            {
                player_detected_error = playerSaysHasError,
                player_was_correct = correct,
                yatzis_delta = delta
            };

            _eventBus.Publish(new ChangeChallengeResolvedEvent(result));
            return result;
        }
    }
}
