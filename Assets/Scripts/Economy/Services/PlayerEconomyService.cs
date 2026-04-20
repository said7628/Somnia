using System.Threading;
using System.Threading.Tasks;
using Somnia.Economy.DTOs;
using Somnia.Economy.Interfaces;

namespace Somnia.Economy.Services
{
    public class PlayerEconomyService
    {
        private readonly IPlayerEconomyApiClient _apiClient;
        private readonly IPlayerSessionProvider _session;

        public PlayerEconomyService(IPlayerEconomyApiClient apiClient, IPlayerSessionProvider session)
        {
            _apiClient = apiClient;
            _session = session;
        }

        public int GetCurrentPlayerId() => _session.CurrentPlayerId;

        public Task<ApiResponse<PlayerEconomyData>> GetPlayerEconomyAsync(CancellationToken ct = default)
        {
            return _apiClient.GetPlayerEconomyAsync(_session.CurrentPlayerId, ct);
        }

        public Task<ApiResponse<EconomyBalanceResponse>> GetBalanceAsync(CancellationToken ct = default)
        {
            return _apiClient.GetBalanceAsync(_session.CurrentPlayerId, ct);
        }

        public Task<ApiResponse<RangeAgeData>> GetRangeAgeAsync(CancellationToken ct = default)
        {
            return _apiClient.GetRangeAgeAsync(_session.CurrentPlayerId, ct);
        }

        public Task<ApiResponse<EconomyBalanceResponse>> UpdateBalanceAsync(int delta, string reason, string source, CancellationToken ct = default)
        {
            var request = new UpdateBalanceRequest
            {
                id_jugador = _session.CurrentPlayerId,
                delta = delta,
                reason = reason,
                source = source
            };
            return _apiClient.UpdateBalanceAsync(request, ct);
        }
    }
}
