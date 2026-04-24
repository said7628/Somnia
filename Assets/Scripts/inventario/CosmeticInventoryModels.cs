using System;
using System.Collections.Generic;
using UnityEngine;

namespace Somnia.Inventory
{
    public enum CosmeticCategory
    {
        Color,
        Ojos,
        Outfit,
        Unknown
    }

    [Serializable]
    public class CosmeticInventoryDefaults
    {
        public int defaultColorItemId = 1;
        public int defaultOjosItemId = 11;
        public int defaultOutfitItemId = 18;

        public int GetDefaultId(CosmeticCategory category)
        {
            return category switch
            {
                CosmeticCategory.Color => defaultColorItemId,
                CosmeticCategory.Ojos => defaultOjosItemId,
                CosmeticCategory.Outfit => defaultOutfitItemId,
                _ => 0
            };
        }
    }

    [Serializable]
    public class CosmeticInventoryItemViewModel
    {
        public int itemId;
        public string name;
        public CosmeticCategory category;
        public string sourceType;
        public bool owned;
        public bool equipped;
        public bool existsInBackendInventory;
        public bool isDefaultFallback;
    }

    [Serializable]
    public class CosmeticInventorySnapshot
    {
        public int slotNumber;
        public int partidaId;
        public IReadOnlyList<CosmeticInventoryItemViewModel> items;
        public Dictionary<CosmeticCategory, int> equippedByCategory;

        public CosmeticInventorySnapshot(int slotNumber, int partidaId, IReadOnlyList<CosmeticInventoryItemViewModel> items, Dictionary<CosmeticCategory, int> equippedByCategory)
        {
            this.slotNumber = slotNumber;
            this.partidaId = partidaId;
            this.items = items;
            this.equippedByCategory = equippedByCategory;
        }
    }

    [CreateAssetMenu(fileName = "CosmeticInventoryIconRegistry", menuName = "Somnia/Inventory/Cosmetic Icon Registry")]
    public class CosmeticInventoryIconRegistry : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public int itemId;
            public Sprite sprite;
        }

        [SerializeField] private List<Entry> entries = new();
        private Dictionary<int, Sprite> lookup;

        public Sprite GetSprite(int itemId)
        {
            if (lookup == null)
            {
                lookup = new Dictionary<int, Sprite>();
                foreach (var entry in entries)
                {
                    if (entry == null || entry.itemId <= 0 || entry.sprite == null)
                    {
                        continue;
                    }

                    lookup[entry.itemId] = entry.sprite;
                }
            }

            return lookup.TryGetValue(itemId, out var sprite) ? sprite : null;
        }
    }
}