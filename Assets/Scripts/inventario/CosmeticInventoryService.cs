using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Somnia.Economy.DTOs;
using Somnia.Economy.Interfaces;
using UnityEngine;

namespace Somnia.Inventory
{
    public sealed class CosmeticInventoryService
    {
        private readonly IGameDataService _gameDataService;
        private readonly CosmeticInventoryDefaults _defaults;
        private readonly IReadOnlyList<int> _catalogIslandIds;

        public CosmeticInventoryService(IGameDataService gameDataService, CosmeticInventoryDefaults defaults, IReadOnlyList<int> catalogIslandIds)
        {
            _gameDataService = gameDataService;
            _defaults = defaults;
            _catalogIslandIds = catalogIslandIds ?? Array.Empty<int>();
        }

        public async Task<CosmeticInventorySnapshot> LoadSnapshotAsync(int slotNumber, CancellationToken ct = default)
        {
            var slotDetailTask = _gameDataService.GetSlotDetailAsync(slotNumber, ct);
            var inventoryTask = _gameDataService.GetInventoryAsync(slotNumber, ct);
            var equippedTask = _gameDataService.GetEquippedItemsAsync(slotNumber, ct);

            await Task.WhenAll(slotDetailTask, inventoryTask, equippedTask);

            var slotDetailResult = await slotDetailTask;
            if (!slotDetailResult.success || slotDetailResult.data?.slot == null)
            {
                throw new InvalidOperationException($"No fue posible obtener detalle del slot {slotNumber}: {slotDetailResult.message}");
            }

            var catalogItems = await LoadCatalogUniverseAsync(slotNumber, ct);
            var rawInventory = inventoryTask.Result.success
                ? inventoryTask.Result.data?.Select(x => x.id_item).ToHashSet() ?? new HashSet<int>()
                : new HashSet<int>();

            var normalizedOwned = NormalizeOwned(rawInventory);
            var equippedByCategory = NormalizeEquipped(equippedTask.Result?.data, normalizedOwned);

            var mapped = catalogItems
                .Select(item => new CosmeticInventoryItemViewModel
                {
                    itemId = item.id,
                    name = string.IsNullOrWhiteSpace(item.nombre) ? $"Item {item.id}" : item.nombre,
                    category = ParseCategory(item.tipo),
                    sourceType = item.tipo,
                    owned = normalizedOwned.Contains(item.id),
                    equipped = false,
                    isDefaultFallback = false
                })
                .Where(x => x.category != CosmeticCategory.Unknown)
                .OrderBy(x => x.category)
                .ThenBy(x => x.itemId)
                .ToList();

            var idsInCatalog = mapped.Select(x => x.itemId).ToHashSet();
            foreach (var category in new[] { CosmeticCategory.Color, CosmeticCategory.Ojos, CosmeticCategory.Outfit })
            {
                int fallbackId = _defaults.GetDefaultId(category);
                if (!idsInCatalog.Contains(fallbackId))
                {
                    mapped.Add(new CosmeticInventoryItemViewModel
                    {
                        itemId = fallbackId,
                        name = $"Default {category}",
                        category = category,
                        sourceType = category.ToString().ToLowerInvariant(),
                        owned = true,
                        equipped = false,
                        isDefaultFallback = true
                    });
                }
            }

            foreach (var item in mapped)
            {
                item.equipped = equippedByCategory.TryGetValue(item.category, out var equippedId) && equippedId == item.itemId;
            }

            return new CosmeticInventorySnapshot(slotNumber, slotDetailResult.data.slot.id_partida, mapped, equippedByCategory);
        }

        public async Task<ApiResponse<EquippedItemsData>> EquipAsync(int slotNumber, CosmeticInventorySnapshot currentSnapshot, CosmeticCategory category, int itemId, CancellationToken ct = default)
        {
            if (currentSnapshot == null)
            {
                return ApiResponse<EquippedItemsData>.Fail("missing_snapshot", "Inventario no inicializado.");
            }

            bool owned = currentSnapshot.items.Any(i => i.itemId == itemId && i.category == category && i.owned);
            if (!owned)
            {
                return ApiResponse<EquippedItemsData>.Fail("item_not_owned", "No puedes equipar un ítem bloqueado.");
            }

            int currentFace = currentSnapshot.equippedByCategory.TryGetValue(CosmeticCategory.Ojos, out var faceId)
                ? faceId
                : _defaults.defaultOjosItemId;
            int currentColor = currentSnapshot.equippedByCategory.TryGetValue(CosmeticCategory.Color, out var colorId)
                ? colorId
                : _defaults.defaultColorItemId;
            int currentOutfit = currentSnapshot.equippedByCategory.TryGetValue(CosmeticCategory.Outfit, out var outfitId)
                ? outfitId
                : _defaults.defaultOutfitItemId;

            if (category == CosmeticCategory.Ojos) currentFace = itemId;
            if (category == CosmeticCategory.Color) currentColor = itemId;
            if (category == CosmeticCategory.Outfit) currentOutfit = itemId;

            var request = new UpdateEquipmentRequest
            {
                id_item_cara = currentFace,
                id_item_color = currentColor,
                id_item_outfit = currentOutfit
            };

            return await _gameDataService.UpdateEquipmentAsync(slotNumber, request, ct);
        }

        private async Task<List<ShopItemViewData>> LoadCatalogUniverseAsync(int slotNumber, CancellationToken ct)
        {
            var all = new Dictionary<int, ShopItemViewData>();
            foreach (var islandId in _catalogIslandIds)
            {
                var result = await _gameDataService.GetShopItemsAsync(islandId, slotNumber, ct);
                if (!result.success || result.data == null)
                {
                    Debug.LogWarning($"[Inventory] No se pudo cargar catálogo de isla {islandId}: {result.message}");
                    continue;
                }

                foreach (var item in result.data)
                {
                    if (item == null || item.id <= 0)
                    {
                        continue;
                    }

                    all[item.id] = item;
                }
            }

            return all.Values.ToList();
        }

        private HashSet<int> NormalizeOwned(HashSet<int> backendOwned)
        {
            backendOwned ??= new HashSet<int>();

            if (backendOwned.Count == 0)
            {
                Debug.LogWarning("[Inventory] inventario_cosmeticos vacío. Se aplican defaults SOLO como fallback visual local.");
            }

            backendOwned.Add(_defaults.defaultColorItemId);
            backendOwned.Add(_defaults.defaultOjosItemId);
            backendOwned.Add(_defaults.defaultOutfitItemId);
            return backendOwned;
        }

        private Dictionary<CosmeticCategory, int> NormalizeEquipped(EquippedItemsData backendEquipped, HashSet<int> owned)
        {
            int colorId = backendEquipped?.id_item_color ?? 0;
            int ojosId = backendEquipped?.id_item_cara ?? 0;
            int outfitId = backendEquipped?.id_item_outfit ?? 0;

            if (!owned.Contains(colorId)) colorId = _defaults.defaultColorItemId;
            if (!owned.Contains(ojosId)) ojosId = _defaults.defaultOjosItemId;
            if (!owned.Contains(outfitId)) outfitId = _defaults.defaultOutfitItemId;

            return new Dictionary<CosmeticCategory, int>
            {
                [CosmeticCategory.Color] = colorId,
                [CosmeticCategory.Ojos] = ojosId,
                [CosmeticCategory.Outfit] = outfitId
            };
        }

        public static CosmeticCategory ParseCategory(string tipo)
        {
            string normalized = (tipo ?? string.Empty).Trim().ToLowerInvariant();
            return normalized switch
            {
                "color" => CosmeticCategory.Color,
                "ojos" => CosmeticCategory.Ojos,
                "cara" => CosmeticCategory.Ojos,
                "outfit" => CosmeticCategory.Outfit,
                _ => CosmeticCategory.Unknown
            };
        }
    }
}
