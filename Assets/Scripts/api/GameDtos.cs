using System;

namespace Somnia.UnityClient
{
    [Serializable]
    public class ApiEnvelope<T>
    {
        public bool success;
        public string message;
        public T payload;
    }

    [Serializable]
    public class ExchangeTicketRequest
    {
        public string ticket;
    }

    [Serializable]
    public class TokenBundle
    {
        public string accessToken;
        public string refreshToken;
        public string accessTtl;
        public int refreshTtlDays;
    }

    [Serializable]
    public class PlayerIdentity
    {
        public int id_usuario;
        public int id_jugador;
        public int id_partida;
        public string usuario;
        public string correo;
        public string rol;
        public int edad_jugador;
    }

    [Serializable]
    public class GameAuthResponse
    {
        public bool success;
        public string message;
        public PlayerIdentity user;
        public TokenBundle tokens;
    }

    [Serializable]
    public class SlotSummaryResponse
    {
        public bool success;
        public SlotSummary[] slots;
    }

    [Serializable]
    public class SlotSummary
    {
        public int slot_numero;
        public bool occupied;
        public SlotCore partida;
    }

    [Serializable]
    public class SlotCore
    {
        public int id_partida;
        public int slot_numero;
        public string nombre_slot;
        public int yatzis;
        public string fecha_creacion;
        public string ultima_actualizacion;
    }

    [Serializable]
    public class SlotDetailResponse
    {
        public bool success;
        public SlotCore slot;
        public int edad_jugador;
        public EquipamientoDto equipamiento;
        public InventarioItemDto[] inventario_cosmeticos;
        public InventoryNewSummaryDto inventory_new_summary;
        public ProgresoDto[] progreso;
        public ProgresoDto[] progress;
        public int[] niveles_completados;
        public CompraDto[] compras;
    }

    [Serializable]
    public class EquipamientoDto
    {
        public int id_partida;
        public int id_item_cara;
        public int id_item_color;
        public int id_item_outfit;
    }

    [Serializable]
    public class InventarioItemDto
    {
        public int id_item;
        public string nombre;
        public string tipo;
        public string descripcion;
        public int nuevo;

        public bool isNew => nuevo == 1;
    }

    [Serializable]
    public class InventoryNewSummaryDto
    {
        public int color;
        public int ojos;
        public int outfit;

        public bool hasNewColor => color == 1;
        public bool hasNewOjos => ojos == 1;
        public bool hasNewOutfit => outfit == 1;
    }

    [Serializable]
    public class ProgresoDto
    {
        public int id_nivel;
        public int completo;
        public int puntuacion_maxima;
        public string ultimo_intento;
        public int id_isla;
        public string nombre;
        public string tipo;
    }

    [Serializable]
    public class CompraDto
    {
        public int id_compra;
        public int id_tienda;
        public string fecha;
        public int total_yatzis;
        public int id_tienda_item;
        public int cantidad;
        public int subtotal;
    }
}