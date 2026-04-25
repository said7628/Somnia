using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Somnia.UnityClient
{
    public static class SlotDetailResponseNormalizer
    {
        private static readonly Regex SummaryObjectRegex = new("\"(inventory_new_summary|inventoryNewSummary)\"\\s*:\\s*\\{(?<summary>[^}]*)\\}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex SummaryFieldRegex = new("\"(?<name>color|ojos|outfit)\"\\s*:\\s*(?<value>true|false|\\d+|\"[^\"]*\")", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex ItemObjectRegex = new("\\{[^{}]*\"id_item\"\\s*:\\s*(?<id>\\d+)[^{}]*\\}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex NewFieldRegex = new("\"(nuevo|is_new|isNew)\"\\s*:\\s*(?<value>true|false|\\d+|\"[^\"]*\")", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static void Normalize(SlotDetailResponse payload, string rawBody)
        {
            if (payload == null || string.IsNullOrWhiteSpace(rawBody))
            {
                return;
            }

            NormalizeSummary(payload, rawBody);
            NormalizeItems(payload, rawBody);
        }

        private static void NormalizeSummary(SlotDetailResponse payload, string rawBody)
        {
            var summaryMatch = SummaryObjectRegex.Match(rawBody);
            if (!summaryMatch.Success)
            {
                return;
            }

            var dto = payload.inventory_new_summary ?? payload.inventoryNewSummary ?? new InventoryNewSummaryDto();
            string summaryRaw = summaryMatch.Groups["summary"].Value;
            var fieldMatches = SummaryFieldRegex.Matches(summaryRaw);
            for (int i = 0; i < fieldMatches.Count; i++)
            {
                var field = fieldMatches[i];
                string name = field.Groups["name"].Value;
                bool value = ParseFlexibleBool(field.Groups["value"].Value);
                switch (name)
                {
                    case "color":
                        dto.color = value;
                        break;
                    case "ojos":
                        dto.ojos = value;
                        break;
                    case "outfit":
                        dto.outfit = value;
                        break;
                }
            }

            payload.inventory_new_summary = dto;
            payload.inventoryNewSummary = dto;
        }

        private static void NormalizeItems(SlotDetailResponse payload, string rawBody)
        {
            var items = payload.inventario_cosmeticos;
            if (items == null || items.Length == 0)
            {
                return;
            }

            var itemMatches = ItemObjectRegex.Matches(rawBody);
            for (int i = 0; i < itemMatches.Count; i++)
            {
                var match = itemMatches[i];
                if (!int.TryParse(match.Groups["id"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int idItem))
                {
                    continue;
                }

                bool? normalizedNew = null;
                var newMatch = NewFieldRegex.Match(match.Value);
                if (newMatch.Success)
                {
                    normalizedNew = ParseFlexibleBool(newMatch.Groups["value"].Value);
                }

                if (!normalizedNew.HasValue)
                {
                    continue;
                }

                for (int itemIndex = 0; itemIndex < items.Length; itemIndex++)
                {
                    var item = items[itemIndex];
                    if (item != null && item.id_item == idItem)
                    {
                        int asInt = normalizedNew.Value ? 1 : 0;
                        item.nuevo = asInt;
                        item.is_new = asInt;
                        item.isNew = asInt;
                    }
                }
            }
        }

        private static bool ParseFlexibleBool(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            string normalized = raw.Trim().Trim('"');
            if (string.Equals(normalized, "true", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(normalized, "false", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return normalized == "1";
        }
    }
}