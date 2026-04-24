using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Somnia.Economy.DTOs;
using Somnia.Economy.Interfaces;
using Somnia.UnityClient;
using UnityEngine;

namespace Somnia.Inventory
{
    public sealed class CosmeticInventoryService
    {
        private sealed class CatalogSeed
        {
            public int id;
            public string nombre;
            public string tipo;
        }

        private static readonly CatalogSeed[] LocalCatalogFallback =
        {
            new() { id = 1, nombre = "blanco", tipo = "color" },
            new() { id = 2, nombre = "naranja", tipo = "color" },
            new() { id = 3, nombre = "morado", tipo = "color" },
            new() { id = 4, nombre = "amarillo", tipo = "color" },
            new() { id = 5, nombre = "rojo", tipo = "color" },
            new() { id = 6, nombre = "turquesa", tipo = "color" },
            new() { id = 7, nombre = "verde", tipo = "color" },
            new() { id = 8, nombre = "rosa", tipo = "color" },
            new() { id = 9, nombre = "azul", tipo = "color" },
            new() { id = 10, nombre = "negro", tipo = "color" },
            new() { id = 11, nombre = "ovalos", tipo = "ojos" },
            new() { id = 12, nombre = "rombos", tipo = "ojos" },
            new() { id = 13, nombre = "cansado", tipo = "ojos" },
            new() { id = 14, nombre = "estrella", tipo = "ojos" },
            new() { id = 15, nombre = "happy", tipo = "ojos" },
            new() { id = 16, nombre = "pirata", tipo = "ojos" },
            new() { id = 17, nombre = "emputado", tipo = "ojos" },
            new() { id = 18, nombre = "boy scout", tipo = "outfit" },
            new() { id = 19, nombre = "engrane", tipo = "outfit" },
            new() { id = 20, nombre = "rana", tipo = "outfit" },
            new() { id = 21, nombre = "diablito", tipo = "outfit" }
        };

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
            var equippedTask = _gameDataService.GetEquippedItemsAsync(slotNumber, ct);

            await Task.WhenAll(slotDetailTask, equippedTask);

            var slotDetailResult = await slotDetailTask;
            if (!slotDetailResult.success || slotDetailResult.data?.slot == null)
            {
                throw new InvalidOperationException($"No fue posible obtener detalle del slot {slotNumber}: {slotDetailResult.message}");
            }

            var catalogItems = await LoadCatalogUniverseAsync(slotNumber, ct);
            var backendInventory = slotDetailResult.data.inventario_cosmeticos ?? Array.Empty<InventarioItemDto>();
            var rawInventory = backendInventory.Select(x => x.id_item).ToHashSet();
            var isNewByItemId = backendInventory.ToDictionary(x => x.id_item, x => x.isNew);

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
                    isNew = isNewByItemId.TryGetValue(item.id, out var isNew) && isNew,
                    existsInBackendInventory = rawInventory.Contains(item.id),
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
                        isNew = false,
                        existsInBackendInventory = rawInventory.Contains(fallbackId),
                        equipped = false,
                        isDefaultFallback = true
                    });
                }
            }

            foreach (var item in mapped)
            {
                item.equipped = equippedByCategory.TryGetValue(item.category, out var equippedId) && equippedId == item.itemId;
            }

            var hasNewByCategory = BuildNewSummary(slotDetailResult.data.inventory_new_summary, mapped);

            return new CosmeticInventorySnapshot(
                slotNumber,
                slotDetailResult.data.slot.id_partida,
                mapped,
                equippedByCategory,
                hasNewByCategory
            );
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
            var all = LocalCatalogFallback.ToDictionary(
                item => item.id,
                item => new ShopItemViewData
                {
                    id = item.id,
                    nombre = item.nombre,
                    tipo = item.tipo
                }
            );

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

                    if (all.TryGetValue(item.id, out var existing))
                    {
                        existing.nombre = string.IsNullOrWhiteSpace(item.nombre) ? existing.nombre : item.nombre;
                        existing.tipo = string.IsNullOrWhiteSpace(item.tipo) ? existing.tipo : item.tipo;
                        existing.costo = item.costo;
                        existing.descripcion = item.descripcion;
                        existing.id_tienda = item.id_tienda;
                        existing.id_tienda_item = item.id_tienda_item;
                    }
                    else
                    {
                        all[item.id] = item;
                    }
                }
            }

            return all.Values.ToList();
        }

        private HashSet<int> NormalizeOwned(HashSet<int> backendOwned)
        {
            backendOwned ??= new HashSet<int>();

            EnsureDefaultOwnedAsFallback(backendOwned, _defaults.defaultColorItemId, "color");
            EnsureDefaultOwnedAsFallback(backendOwned, _defaults.defaultOjosItemId, "ojos");
            EnsureDefaultOwnedAsFallback(backendOwned, _defaults.defaultOutfitItemId, "outfit");

            return backendOwned;
        }

        private static void EnsureDefaultOwnedAsFallback(HashSet<int> owned, int defaultItemId, string categoryLabel)
        {
            if (owned.Contains(defaultItemId))
            {
                return;
            }

            Debug.LogWarning(
                $"[Inventory] backend inventario_cosmeticos no incluye default de {categoryLabel} (id={defaultItemId}). Se agrega fallback local de seguridad."
            );
            owned.Add(defaultItemId);
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

        private static Dictionary<CosmeticCategory, bool> BuildNewSummary(
            InventoryNewSummaryDto backendSummary,
            IReadOnlyList<CosmeticInventoryItemViewModel> items
        )
        {
            if (backendSummary != null)
            {
                return new Dictionary<CosmeticCategory, bool>
                {
                    [CosmeticCategory.Color] = backendSummary.hasNewColor,
                    [CosmeticCategory.Ojos] = backendSummary.hasNewOjos,
                    [CosmeticCategory.Outfit] = backendSummary.hasNewOutfit
                };
            }

            return new Dictionary<CosmeticCategory, bool>
            {
                [CosmeticCategory.Color] = items.Any(i => i.category == CosmeticCategory.Color && i.isNew),
                [CosmeticCategory.Ojos] = items.Any(i => i.category == CosmeticCategory.Ojos && i.isNew),
                [CosmeticCategory.Outfit] = items.Any(i => i.category == CosmeticCategory.Outfit && i.isNew)
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