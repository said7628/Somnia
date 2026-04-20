using Somnia.Economy.Core;
using Somnia.Economy.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Somnia.Economy.UI
{
    
    /// Adapter de UI: escucha eventos del modulo y actualiza vistas.
    
    public class EconomyUIFacade : MonoBehaviour
    {
        [SerializeField] private EconomyModule economyModule;
        [SerializeField] private Text balanceText;
        [SerializeField] private Text feedbackText;

        private void OnEnable()
        {
            if (economyModule == null)
            {
                Debug.LogWarning("EconomyUIFacade sin EconomyModule asignado.");
                return;
            }

            economyModule.EventBus.OnCurrencyChanged += HandleCurrencyChanged;
            economyModule.EventBus.OnPurchaseCompleted += HandlePurchaseCompleted;
            economyModule.EventBus.OnPurchaseFailed += HandlePurchaseFailed;
            economyModule.EventBus.OnChangeChallengeResolved += HandleChangeResolved;
            RefreshBalance();
        }

        private void OnDisable()
        {
            if (economyModule == null || economyModule.EventBus == null) return;
            economyModule.EventBus.OnCurrencyChanged -= HandleCurrencyChanged;
            economyModule.EventBus.OnPurchaseCompleted -= HandlePurchaseCompleted;
            economyModule.EventBus.OnPurchaseFailed -= HandlePurchaseFailed;
            economyModule.EventBus.OnChangeChallengeResolved -= HandleChangeResolved;
        }

        private void HandleCurrencyChanged(CurrencyChangedEvent evt)
        {
            if (balanceText != null)
            {
                balanceText.text = $"Yatzis: {evt.CurrentBalance}";
            }
        }

        private void HandlePurchaseCompleted(PurchaseCompletedEvent evt)
        {
            SetFeedback($"Compra completada. Item #{evt.ItemId}");
        }

        private void HandlePurchaseFailed(PurchaseFailedEvent evt)
        {
            SetFeedback($"Compra fallida: {evt.Reason}");
        }

        private void HandleChangeResolved(ChangeChallengeResolvedEvent evt)
        {
            SetFeedback(evt.Result.player_was_correct
                ? $"¡Sistema de cambio correcto! +{evt.Result.yatzis_delta} Yatzis"
                : $"Sistema de cambio fallido: {evt.Result.yatzis_delta} Yatzis");
        }

        private void SetFeedback(string message)
        {
            if (feedbackText != null)
            {
                feedbackText.text = message;
            }
        }

        private void RefreshBalance()
        {
            if (economyModule?.CurrencyManager != null)
            {
                balanceText.text = $"Yatzis: {economyModule.CurrencyManager.Balance}";
            }
        }
    }
}
