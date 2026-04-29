using System;
using System.Collections.Generic;

namespace Somnia.Economy.DTOs
{
    [Serializable]
    public class PlayerEconomyData
    {
        public int id_jugador;
        public int id_usuario;
        public int id_rango_edad;
        public int yatzis;
        public RangeAgeData rango_edad;
    }

    [Serializable]
    public class EconomyBalanceResponse
    {
        public int id_jugador;
        public int yatzis;
        public DateTime updated_at;
    }

    [Serializable]
    public class UpdateBalanceRequest
    {
        public int id_jugador;
        public int delta;
        public string reason;
        public string source;
    }

    [Serializable]
    public class ShopItemData
    {
        public int id_tienda_item;
        public int id_tienda;
        public int id_item;
        public string nombre;
        public string tipo;
        public string descripcion;
        public int costo;
        public int cantidad_max;
        public int id_isla;
        public int required_level_id;
    }

    [Serializable]
    public class ShopCatalogResponse
    {
        public int id_tienda;
        public int id_isla;
        public string nombre_tienda;
        public List<ShopItemData> items = new();
    }

    [Serializable]
    public class PurchaseRequest
    {
        public int id_jugador;
        public int id_tienda;
        public int id_tienda_item;
        public int cantidad = 1;
        public bool change_challenge_enabled;
        public ChangeChallengeResult change_result;
    }

    [Serializable]
    public class PurchaseResponse
    {
        public int id_compra;
        public int id_jugador;
        public int total_yatzis;
        public int balance_after;
        public DateTime fecha;
        public List<PurchaseDetailData> detalles = new();
    }

    [Serializable]
    public class PurchaseDetailData
    {
        public int id_compra;
        public int id_tienda;
        public int id_tienda_item;
        public int cantidad;
        public int subtotal;
    }

    [Serializable]
    public class ChangeChallengeData
    {
        public int purchase_cost;
        public int amount_paid;
        public int real_change;
        public int displayed_change;
        public bool has_error;
        public int error_percentage;
    }

    [Serializable]
    public class ChangeChallengeResult
    {
        public bool player_detected_error;
        public bool player_was_correct;
        public int yatzis_delta;
    }

    [Serializable]
    public class LevelRewardData
    {
        public int level_id;
        public bool is_math_level;
        public int fixed_reward;
        public int max_score;
        public float score_to_yatzis_ratio;
    }

    [Serializable]
    public class ProgressData
    {
        public int id_jugador;
        public int id_nivel;
        public bool completo;
        public int puntuacion_maxima;
        public DateTime ultimo_intento;
        public int delta_yatzis;
        public int reward_yatzis;
    }

    [Serializable]
    public class IslandData
    {
        public int id_isla;
        public string nombre;
        public float bono_isla;
        public int orden;
    }

    [Serializable]
    public class RangeAgeData
    {
        public int id_rango_edad;
        public int edad_min;
        public int numero;
        public float multiplicador;
    }
}