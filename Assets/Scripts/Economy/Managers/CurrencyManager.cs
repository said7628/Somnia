using System;
using System.Threading;
using System.Threading.Tasks;
using Somnia.Economy.Events;
using Somnia.Economy.Services;
using UnityEngine;

namespace Somnia.Economy.Managers
{
    public class CurrencyManager
    {
        private readonly PlayerEconomyService _economyService;
        private readonly EconomyEventBus _eventBus;
        private int _balance;

        public CurrencyManager(PlayerEconomyService economyService, EconomyEventBus eventBus)
        {
            _economyService = economyService;
            _eventBus = eventBus;
        }

        public int Balance => _balance;

        public async Task<bool> InitializeAsync(CancellationToken ct = default)
        {
            var response = await _economyService.GetBalanceAsync(ct);
            if (!response.success || response.data == null)
            {
                Debug.LogWarning($"CurrencyManager Initialize failed: {response.message}");
                return false;
            }

            SetBalance(response.data.yatzis, "initial_sync");
            return true;
        }

        public bool CanAfford(int amount) => amount >= 0 && _balance >= amount;

        public async Task<bool> AddYatzisAsync(int amount, string reason, string source = "economy", CancellationToken ct = default)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            return await ChangeBalanceAsync(amount, reason, source, ct);
        }

        public async Task<bool> SpendYatzisAsync(int amount, string reason, string source = "economy", CancellationToken ct = default)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (!CanAfford(amount)) return false;
            return await ChangeBalanceAsync(-amount, reason, source, ct);
        }

        public void ForceSetLocalBalance(int balance, string reason = "manual")
        {
            SetBalance(Mathf.Max(0, balance), reason);
        }

        private async Task<bool> ChangeBalanceAsync(int delta, string reason, string source, CancellationToken ct)
        {
            var response = await _economyService.UpdateBalanceAsync(delta, reason, source, ct);
            if (!response.success || response.data == null)
            {
                Debug.LogWarning($"Currency sync failed: {response.message}");
                return false;
            }

            SetBalance(response.data.yatzis, reason);
            return true;
        }

        private void SetBalance(int newBalance, string reason)
        {
            var previous = _balance;
            _balance = Mathf.Max(0, newBalance);
            _eventBus.Publish(new CurrencyChangedEvent(previous, _balance, reason));
        }
    }
}
