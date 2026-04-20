using System;
using Somnia.Economy.DTOs;

namespace Somnia.Economy.Events
{
    public class EconomyEventBus
    {
        public event Action<CurrencyChangedEvent> OnCurrencyChanged;
        public event Action<RewardGrantedEvent> OnRewardGranted;
        public event Action<PurchaseStartedEvent> OnPurchaseStarted;
        public event Action<PurchaseCompletedEvent> OnPurchaseCompleted;
        public event Action<PurchaseFailedEvent> OnPurchaseFailed;
        public event Action<ChangeChallengeStartedEvent> OnChangeChallengeStarted;
        public event Action<ChangeChallengeResolvedEvent> OnChangeChallengeResolved;

        public void Publish(CurrencyChangedEvent evt) => OnCurrencyChanged?.Invoke(evt);
        public void Publish(RewardGrantedEvent evt) => OnRewardGranted?.Invoke(evt);
        public void Publish(PurchaseStartedEvent evt) => OnPurchaseStarted?.Invoke(evt);
        public void Publish(PurchaseCompletedEvent evt) => OnPurchaseCompleted?.Invoke(evt);
        public void Publish(PurchaseFailedEvent evt) => OnPurchaseFailed?.Invoke(evt);
        public void Publish(ChangeChallengeStartedEvent evt) => OnChangeChallengeStarted?.Invoke(evt);
        public void Publish(ChangeChallengeResolvedEvent evt) => OnChangeChallengeResolved?.Invoke(evt);
    }

    public readonly struct CurrencyChangedEvent
    {
        public CurrencyChangedEvent(int previousBalance, int currentBalance, string reason)
        {
            PreviousBalance = previousBalance;
            CurrentBalance = currentBalance;
            Reason = reason;
        }
        public int PreviousBalance { get; }
        public int CurrentBalance { get; }
        public string Reason { get; }
    }

    public readonly struct RewardGrantedEvent
    {
        public RewardGrantedEvent(string source, int amount, int levelId)
        {
            Source = source;
            Amount = amount;
            LevelId = levelId;
        }
        public string Source { get; }
        public int Amount { get; }
        public int LevelId { get; }
    }

    public readonly struct PurchaseStartedEvent
    {
        public PurchaseStartedEvent(int itemId, int price)
        {
            ItemId = itemId;
            Price = price;
        }
        public int ItemId { get; }
        public int Price { get; }
    }

    public readonly struct PurchaseCompletedEvent
    {
        public PurchaseCompletedEvent(PurchaseResponse response, int itemId)
        {
            Response = response;
            ItemId = itemId;
        }

        public PurchaseResponse Response { get; }
        public int ItemId { get; }
    }

    public readonly struct PurchaseFailedEvent
    {
        public PurchaseFailedEvent(int itemId, string reason)
        {
            ItemId = itemId;
            Reason = reason;
        }
        public int ItemId { get; }
        public string Reason { get; }
    }

    public readonly struct ChangeChallengeStartedEvent
    {
        public ChangeChallengeStartedEvent(ChangeChallengeData challenge)
        {
            Challenge = challenge;
        }
        public ChangeChallengeData Challenge { get; }
    }

    public readonly struct ChangeChallengeResolvedEvent
    {
        public ChangeChallengeResolvedEvent(ChangeChallengeResult result)
        {
            Result = result;
        }
        public ChangeChallengeResult Result { get; }
    }
}
