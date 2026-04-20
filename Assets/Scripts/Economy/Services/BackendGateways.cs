using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Somnia.Economy.DTOs;
using Somnia.Economy.Interfaces;

namespace Somnia.Economy.Services
{
    public class BackendInventoryGateway : IInventoryGateway
    {
        private readonly Func<IGameDataService> _gameDataService;
        private readonly IPlayerSessionProvider _session;
        private HashSet<int> _cachedItems = new();

        public BackendInventoryGateway(Func<IGameDataService> gameDataService, IPlayerSessionProvider session)
        {
            _gameDataService = gameDataService;
            _session = session;
        }

        public async Task<bool> HasItemAsync(int playerId, int itemId, CancellationToken ct = default)
        {
            if (_cachedItems.Count == 0)
            {
                await RefreshInventoryAsync(playerId, ct);
            }

            return _cachedItems.Contains(itemId);
        }

        public Task AddItemAsync(int playerId, int itemId, CancellationToken ct = default)
        {
            _cachedItems.Add(itemId);
            return Task.CompletedTask;
        }

        public async Task RefreshInventoryAsync(int playerId, CancellationToken ct = default)
        {
            var gameData = _gameDataService();
            if (gameData == null)
            {
                _cachedItems = new HashSet<int>();
                return;
            }

            var response = await gameData.GetInventoryAsync(_session.CurrentSlotNumber, ct);
            _cachedItems = response.success
                ? response.data.Select(i => i.id_item).ToHashSet()
                : new HashSet<int>();
        }
    }

    public class BackendProgressionGateway : IProgressionGateway
    {
        private readonly Func<IGameDataService> _gameDataService;
        private readonly IPlayerSessionProvider _session;

        public BackendProgressionGateway(Func<IGameDataService> gameDataService, IPlayerSessionProvider session)
        {
            _gameDataService = gameDataService;
            _session = session;
        }

        public async Task<bool> IsLevelCompletedAsync(int playerId, int levelId, CancellationToken ct = default)
        {
            var progress = await GetPlayerProgressAsync(playerId, ct);
            return progress.Any(p => p.id_nivel == levelId && p.completo);
        }

        public async Task MarkLevelCompletedAsync(int playerId, int levelId, int maxScore, CancellationToken ct = default)
        {
            var progress = (await GetPlayerProgressAsync(playerId, ct)).ToList();
            var entry = progress.FirstOrDefault(p => p.id_nivel == levelId);
            if (entry == null)
            {
                progress.Add(new ProgressData { id_jugador = playerId, id_nivel = levelId, completo = true, puntuacion_maxima = maxScore });
            }
            else
            {
                entry.completo = true;
                entry.puntuacion_maxima = entry.puntuacion_maxima < maxScore ? maxScore : entry.puntuacion_maxima;
            }

            var gameData = _gameDataService();
            if (gameData != null)
            {
                await gameData.SaveProgressAsync(_session.CurrentSlotNumber, progress, ct);
            }
        }

        public async Task<IReadOnlyList<ProgressData>> GetPlayerProgressAsync(int playerId, CancellationToken ct = default)
        {
            var gameData = _gameDataService();
            if (gameData == null)
            {
                return new List<ProgressData>();
            }

            var response = await gameData.LoadProgressAsync(_session.CurrentSlotNumber, ct);
            return response.success ? response.data : new List<ProgressData>();
        }

        public async Task<bool> IsIslandUnlockedAsync(int playerId, int islandId, CancellationToken ct = default)
        {
            if (islandId <= 1) return true;

            var progress = await GetPlayerProgressAsync(playerId, ct);
            return progress.Any(p => p.completo);
        }
    }
}
