using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Somnia.Economy.DTOs;

namespace Somnia.Economy.Interfaces
{
    public interface IInventoryGateway
    {
        Task<bool> HasItemAsync(int playerId, int itemId, CancellationToken ct = default);
        Task AddItemAsync(int playerId, int itemId, CancellationToken ct = default);
        Task RefreshInventoryAsync(int playerId, CancellationToken ct = default);
    }

    public interface IProgressionGateway
    {
        Task<bool> IsLevelCompletedAsync(int playerId, int levelId, CancellationToken ct = default);
        Task MarkLevelCompletedAsync(int playerId, int levelId, int maxScore, CancellationToken ct = default);
        Task<IReadOnlyList<ProgressData>> GetPlayerProgressAsync(int playerId, CancellationToken ct = default);
        Task<bool> IsIslandUnlockedAsync(int playerId, int islandId, CancellationToken ct = default);
    }

    public interface IPlayerSessionProvider
    {
        int CurrentUserId { get; }
        int CurrentPlayerId { get; }
        string Token { get; }
        bool IsAuthenticated { get; }
        int CurrentSlotNumber { get; }
    }
}
