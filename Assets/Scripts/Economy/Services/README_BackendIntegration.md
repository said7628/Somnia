# Backend Integration - GameDataService

## Clase principal
- **Script:** `Assets/Scripts/Economy/Services/GameDataService.cs`
- **Responsabilidad:** orquestar acceso a API/DB para slots, economía, tienda, inventario, equipamiento y progreso.

## Funciones reutilizables

### `GetSlotsAsync()`
- **Qué hace:** obtiene todos los slots del jugador autenticado.
- **Parámetros:** ninguno.
- **Devuelve:** `ApiResponse<SlotSummary[]>`.
- **Endpoint:** `GET /game/slots`.
- **Cuándo usarla:** para poblar pantalla de selección de partida.
- **Ejemplo:** `var slots = await economyModule.GameDataService.GetSlotsAsync();`

### `CreateSlotAsync(int slotNumber, string slotName)`
- **Qué hace:** crea slot/partida base.
- **Parámetros:** número de slot, nombre.
- **Devuelve:** `ApiResponse<SlotDetailResponse>`.
- **Endpoint:** `POST /game/slots`.
- **Cuándo usarla:** al pulsar "Nueva partida".
- **Ejemplo:** `await gameData.CreateSlotAsync(2, "Slot 2");`

### `InitializeNewGameAsync(int slotNumber, string slotName)`
- **Qué hace:** flujo central de nueva partida: crea slot, inicializa backend y garantiza Yatzis iniciales.
- **Parámetros:** slot, nombre.
- **Devuelve:** `ApiResponse<SlotDetailResponse>` con estado final.
- **Endpoints:** `POST /game/slots`, `POST /game/slots/{slot}/initialize`, `PUT /economy/players/{id}/balance` (si requiere reset).
- **Cuándo usarla:** único punto para creación+inicialización.
- **Ejemplo:** `var init = await gameData.InitializeNewGameAsync(1, "Nueva Aventura");`

### `GetSlotDetailAsync(int slotNumber)`
- **Qué hace:** carga detalle completo de partida/slot.
- **Devuelve:** `slot`, `inventario_cosmeticos`, `equipamiento`, `progreso`, `compras`.
- **Endpoint:** `GET /game/slots/{slot}`.
- **Ejemplo:** `var detail = await gameData.GetSlotDetailAsync(1);`

### `GetBalanceAsync()`
- **Qué hace:** obtiene balance de Yatzis desde backend.
- **Devuelve:** `ApiResponse<EconomyBalanceResponse>`.
- **Endpoint:** `GET /economy/players/{id}/balance`.
- **Ejemplo:** `var balance = await gameData.GetBalanceAsync();`

### `SaveYatzisAsync(int targetYatzis, string reason)`
- **Qué hace:** sincroniza balance destino (calcula delta y persiste en API).
- **Endpoint:** `PUT /economy/players/{id}/balance`.
- **Cuándo usarla:** ajustes administrativos o reseteo controlado.
- **Ejemplo:** `await gameData.SaveYatzisAsync(0, "reset");`

### `GetShopItemsAsync(int islandId, int slotNumber)`
- **Qué hace:** combina catálogo de tienda + inventario/equipamiento para flags `owned` y `equipped`.
- **Devuelve:** `ApiResponse<IReadOnlyList<ShopItemViewData>>`.
- **Endpoint:** `GET /shops/islands/{island}/catalog?playerId={id}` + `GET /game/slots/{slot}`.
- **Ejemplo:** `var items = await gameData.GetShopItemsAsync(1, 1);`

### `PurchaseItemAsync(...)`
- **Qué hace:** compra item contra backend, descuenta balance, registra compra e inventario.
- **Endpoint:** `POST /shops/purchases` + economía balance endpoint.
- **Cuándo usarla:** botón de compra en UI.

### `GetInventoryAsync(int slotNumber)`
- **Qué hace:** obtiene inventario cosmético del slot.
- **Endpoint:** `GET /game/slots/{slot}`.

### `GetEquippedItemsAsync(int slotNumber)`
- **Qué hace:** obtiene equipamiento actual del slot.
- **Endpoint:** `GET /game/slots/{slot}`.

### `UpdateEquipmentAsync(int slotNumber, UpdateEquipmentRequest request)`
- **Qué hace:** persiste equipamiento seleccionado.
- **Endpoint esperado:** `PUT /game/slots/{slot}/equipment`.
- **Nota:** si backend no expone este endpoint, debe habilitarse.

### `LoadProgressAsync(int slotNumber)`
- **Qué hace:** carga progreso desde backend.
- **Endpoint:** `GET /game/slots/{slot}`.

### `SaveProgressAsync(int slotNumber, IReadOnlyList<ProgressData> progress)`
- **Qué hace:** guarda progreso consolidado.
- **Endpoint esperado:** `PUT /game/slots/{slot}/progress`.
- **Nota:** si backend no lo expone, debe agregarse en API.

## Arquitectura conectada
- UI -> `EconomyModule.GameDataService`.
- `GameDataService` coordina API clients y managers de economía.
- `BackendInventoryGateway` y `BackendProgressionGateway` leen/escriben backend (sin mocks) cuando `useMockGateways=false`.

