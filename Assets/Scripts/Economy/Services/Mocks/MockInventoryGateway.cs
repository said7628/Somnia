using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Somnia.Economy.Interfaces;

namespace Somnia.Economy.Services.Mocks
{
    public class MockInventoryGateway : IInventoryGateway
    {
        private readonly Dictionary<int, HashSet<int>> _inventoryByPlayer = new();

        public Task<bool> HasItemAsync(int playerId, int itemId, CancellationToken ct = default)
        {
            return Task.FromResult(_inventoryByPlayer.TryGetValue(playerId, out var items) && items.Contains(itemId));
        }

        public Task AddItemAsync(int playerId, int itemId, CancellationToken ct = default)
        {
            if (!_inventoryByPlayer.TryGetValue(playerId, out var items))
            {
                items = new HashSet<int>();
                _inventoryByPlayer[playerId] = items;
            }
            items.Add(itemId);
            return Task.CompletedTask;
        }

        public Task RefreshInventoryAsync(int playerId, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }
    }
}
