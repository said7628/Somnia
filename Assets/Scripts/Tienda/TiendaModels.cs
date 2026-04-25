using System;
using System.Collections.Generic;

namespace Somnia.Tienda
{
    public enum ShopCategory
    {
        Color,
        Ojos,
        Outfit,
        Unknown
    }

    [Serializable]
    public class ShopItemDto
    {
        public int id_tienda_item;
        public int id_tienda;
        public int id_item;
        public string nombre;
        public string tipo;
        public string descripcion;
        public int costo;
        public int cantidad_max;
        public bool owned;
        public bool sold_out;

        public ShopCategory ResolveCategory()
        {
            if (string.IsNullOrWhiteSpace(tipo))
            {
                return ShopCategory.Unknown;
            }

            switch (tipo.Trim().ToLowerInvariant())
            {
                case "color":
                    return ShopCategory.Color;
                case "ojos":
                    return ShopCategory.Ojos;
                case "outfit":
                    return ShopCategory.Outfit;
                default:
                    return ShopCategory.Unknown;
            }
        }

        public bool IsSoldOut => owned || sold_out;
    }

    [Serializable]
    public class ShopResponseDto
    {
        public bool success;
        public int slot_numero;
        public int id_partida;
        public int yatzis;
        public List<ShopItemDto> items = new();
        public string message;
    }

    [Serializable]
    public class BuyShopItemRequestDto
    {
        public int id_tienda_item;
    }

    [Serializable]
    public class BuyShopItemResultDto
    {
        public int id_partida;
        public int id_item;
        public int id_tienda_item;
        public int yatzis;
        public int nuevo;
        public bool owned;
        public bool sold_out;
    }

    [Serializable]
    public class BuyShopItemResponseDto
    {
        public bool success;
        public string message;
        public BuyShopItemResultDto data;
    }
}