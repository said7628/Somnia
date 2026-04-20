using Somnia.Economy.Interfaces;
using Somnia.UnityClient;

namespace Somnia.Economy.Services.ApiClients
{
    public class GameSessionPlayerProvider : IPlayerSessionProvider
    {
        public int CurrentUserId => GameSessionManager.Instance?.CurrentUser?.id_usuario ?? 0;
        public int CurrentPlayerId => GameSessionManager.Instance?.CurrentUser?.id_jugador ?? 0;
        public string Token => GameSessionManager.Instance?.AccessToken;
        public bool IsAuthenticated => GameSessionManager.Instance != null && GameSessionManager.Instance.HasSession();
        public int CurrentSlotNumber => GameSessionManager.Instance?.CurrentSlotNumber ?? 1;

        public void SetActiveSlotNumber(int slotNumber)
        {
            GameSessionManager.Instance?.SetCurrentSlot(slotNumber);
        }
    }
}
