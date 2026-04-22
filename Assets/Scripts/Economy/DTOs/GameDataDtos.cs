using System;
using System.Collections.Generic;

namespace Somnia.Economy.DTOs
{
    [Serializable]
    public class CreateSlotRequest
    {
        public int slot_numero;
        public string nombre_slot;
    }

    [Serializable]
    public class InitializeGameRequest
    {
        public bool force_reset_yatzis;
    }

    [Serializable]
    public class SaveProgressRequest
    {
        public List<ProgressData> progreso = new();
    }

    [Serializable]
    public class ProgressSaveResult
    {
        public bool success;
        public string message;
        public int id_partida;
        public int slot_numero;
        public int id_nivel;
        public int completo;
        public int puntuacion_maxima;
        public string ultima_actualizacion;
    }

    [Serializable]
    public class UpdateEquipmentRequest
    {
        public int id_item_cara;
        public int id_item_color;
        public int id_item_outfit;
    }

    [Serializable]
    public class ShopItemViewData
    {
        public int id;
        public string nombre;
        public int costo;
        public string tipo;
        public string descripcion;
        public bool owned;
        public bool equipped;
        public int id_tienda;
        public int id_tienda_item;
    }

    [Serializable]
    public class EquippedItemsData
    {
        public int id_item_cara;
        public int id_item_color;
        public int id_item_outfit;

        public bool IsEquipped(int itemId)
        {
            return id_item_cara == itemId || id_item_color == itemId || id_item_outfit == itemId;
        }
    }
}