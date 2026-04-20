using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Somnia.Economy.DTOs;
using Somnia.Economy.Interfaces;

namespace Somnia.Economy.Services.Mocks
{
    public class MockProgressionGateway : IProgressionGateway
    {
        private readonly Dictionary<int, List<ProgressData>> _progress = new();
        private readonly HashSet<(int playerId, int islandId)> _unlockedIslands = new();

        public Task<bool> IsLevelCompletedAsync(int playerId, int levelId, CancellationToken ct = default)
        {
            var completed = _progress.TryGetValue(playerId, out var data) && data.Any(p => p.id_nivel == levelId && p.completo);
            return Task.FromResult(completed);
        }

        public Task MarkLevelCompletedAsync(int playerId, int levelId, int maxScore, CancellationToken ct = default)
        {
            if (!_progress.TryGetValue(playerId, out var data))
            {
                data = new List<ProgressData>();
                _progress[playerId] = data;
            }

            var entry = data.FirstOrDefault(p => p.id_nivel == levelId);
            if (entry == null)
            {
                data.Add(new ProgressData { id_jugador = playerId, id_nivel = levelId, completo = true, puntuacion_maxima = maxScore });
            }
            else
            {
                entry.completo = true;
                entry.puntuacion_maxima = entry.puntuacion_maxima < maxScore ? maxScore : entry.puntuacion_maxima;
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ProgressData>> GetPlayerProgressAsync(int playerId, CancellationToken ct = default)
        {
            _progress.TryGetValue(playerId, out var data);
            return Task.FromResult((IReadOnlyList<ProgressData>)(data ?? new List<ProgressData>()));
        }

        public Task<bool> IsIslandUnlockedAsync(int playerId, int islandId, CancellationToken ct = default)
        {
            return Task.FromResult(_unlockedIslands.Contains((playerId, islandId)) || islandId == 1);
        }

        public void UnlockIsland(int playerId, int islandId) => _unlockedIslands.Add((playerId, islandId));
    }
}
