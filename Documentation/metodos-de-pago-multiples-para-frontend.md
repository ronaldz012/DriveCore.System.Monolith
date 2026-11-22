# Métodos de pago múltiples — guía de actualización del frontend (por pasos revisables)

El backend ya soporta **pagos divididos** (split payments): ventas y devoluciones tienen
varias líneas de pago (`payments[]`), una por método. Este documento divide la actualización
en **pasos revisables en orden**: primero toda la infraestructura de **lectura** (para que los
datos existentes se vean bien), y **al final** los POST (crear venta / crear devolución).

> **Flujo:** cada paso viene con los archivos backend para revisión → se aprueba → el frontend
> lo aplica. No avanzar al siguiente paso sin aprobar el anterior.

---

## Reglas de oro

1. **`payments[]` viene en TODAS las respuestas** de ventas y devoluciones, y es
   **obligatorio** en los POST. No existe valor por defecto ni espejo automático.
2. **Los montos del request son positivos** ("Bs 60 por QR"); el backend niega el signo
   al guardar devoluciones. En las respuestas, las líneas de devoluciones vienen **negativas**.
3. **La suma debe cerrar exacta** contra el total que calcula el backend (precios de catálogo).
4. **Un método por línea** (`{ method, amount }`): validado en DTO, dominio e índice único
   `(TenantId, SaleId, Method)` en la DB.
5. **`paymentMethod` YA NO EXISTE en ninguna respuesta.** Reemplazar toda lectura por
   `payments[]`. Si se quiere mostrar un método "principal", es el de mayor `amount`.
6. La columna vieja sigue viviendo **solo dentro de la DB** (por compatibilidad hasta que
   sea seguro eliminarla) — **invisible para la API**: no enviarla, no leerla.

### Glosario

| Término | Significado |
|---|---|
| **tender / línea de pago** | Un `{ method, amount }` dentro de `payments[]` |
| **reembolso** | Las líneas de una devolución (negativas en responses, positivas en el request) |
| **método dominante** | El tender de mayor monto — lo que muestra la UI como "principal" si hace falta |

---

# ETAPA A — Lecturas (datos existentes correctos)

## Paso 1 — Listado de ventas: `payments[]` por fila

### Archivos backend para revisión

| Archivo | Qué revisar |
|---|---|
| `src/modules/Module.Sales/Application/UseCases/Sales/Get/SaleListDto.cs` | ❌ `paymentMethod` eliminado · ✅ `payments[]` (`{ method, amount }`) |
| `src/modules/Module.Sales/Application/UseCases/Sales/Get/GetListSales.cs` | proyección de las líneas por fila |

### Contrato

```jsonc
GET /api/Sale
{
  "items": [
    {
      "number": 46,
      "totalAmount": 135.00,
      "payments": [
        { "method": 1, "amount": 85.00 },
        { "method": 0, "amount": 50.00 }
      ],
      // "paymentMethod": ❌ ELIMINADO
      "type": 0, "hasReturn": false, "returnStatus": 0, "…": "…"
    }
  ]
}
```

### Tareas del frontend

- [ ] Reemplazar toda lectura de `paymentMethod` en la fila por `payments[]`.
- [ ] Display: 1 línea → método + monto como hoy; varias → compuesto ("QR + Efectivo")
      o el de mayor monto como principal.

### Criterio de aceptación

- Ventas viejas (1 línea por el backfill) se listan igual que antes.
- Una venta split muestra ambos métodos; nada referencia `paymentMethod`.
- **Las devoluciones también se listan por aquí** (`type: 1` / `returnStatus`): sus
  `payments[]` vienen con montos **negativos** — el listado exclusivo de devoluciones
  (`GET /api/Sale/returns`) fue **eliminado del backend**.

---

## Paso 2 — Detalle de venta: `payments[]`

### Archivos backend para revisión

| Archivo | Qué revisar |
|---|---|
| `src/modules/Module.Sales/Application/UseCases/Sales/GetById/SaleDetailDto.cs` | ❌ `paymentMethod` eliminado · ✅ `payments[]` |
| `src/modules/Module.Sales/Application/UseCases/Sales/GetById/GetSaleDetails.cs` | proyección de las líneas |

### Contrato

```jsonc
GET /api/Sale/{id}/details
{
  "payments": [ { "method": 1, "amount": 85.00 }, { "method": 0, "amount": 50.00 } ],
  "items": [ … ], "returns": [ … ], "canReturn": true, "returnStatus": 0
}
```

### Tareas del frontend

- [ ] Mostrar los tenders (lista o compuesto); las devoluciones de `returns[]` son documentos
      completos con sus propias líneas (negativas).

### Criterio de aceptación

- El detalle de una venta split muestra los dos tenders con sus montos.

---

## Paso 3 — Cierres de caja: filas con `payments[]`, totales sin cambios

### Archivos backend para revisión

| Archivo | Qué confirmar |
|---|---|
| `src/modules/Module.Sales/Application/UseCases/Registers/GetById/ClosureDetailDto.cs` | filas de venta: ❌ `paymentMethod` · ✅ `payments[]` |
| `src/modules/Module.Sales/Application/UseCases/Registers/GetById/GetClosureDetail.cs` | `cashSales`/`qrSales` **calculados por líneas** — mismos nombres de campo |
| `src/modules/Module.Sales/Application/UseCases/Registers/Close/CloseCashRegister.cs` | `expectedCash` por líneas |
| `src/modules/Module.Sales/Application/UseCases/Registers/List/ListClosures.cs` | `cashSales` por líneas |
| `src/modules/Module.Sales/Application/UseCases/Registers/TodaySales/GetTodaySales.cs` | `cashAmount`/`qrCodeAmount` por líneas |
| `src/modules/Module.Sales/Application/UseCases/Sales/Stats/GetSalesStats.cs` | `byPaymentMethod` por líneas |

### Contrato

- **Totales del cierre: sin cambios de forma** (`cashSales`, `qrSales`, `totalSales`,
  `systemSalesAmount`, `expectedCash`, `cashAmount`…) — solo se calculan por líneas ahora:
  una venta `{QR 85, Efectivo 50}` ya no infla el QR con 135.
- **Filas de venta dentro del cierre**: `payments[]` en cada una, `paymentMethod` eliminado.

### Tareas del frontend

- [ ] Reemplazar `paymentMethod` de cada fila del cierre por `payments[]`.
- [ ] Totales: sin tocar (mismos campos).

### Criterio de aceptación

- Cierres viejos recalculan idéntico (verificado contra la DB: 0 diferencias).
- Cierre con una venta split: efectivo y QR con sus montos reales; el arqueo cuadra.

---

# Componente de pago — modal compartido (lo usan el Paso 4 y el Paso 5)

## Layout

```
DESKTOP (≈60/40)                          CELULAR (una columna)
┌──────────────────┬──────────────┐        ┌──────────────┐
│ LISTA (60%)      │ PAGO (40%)   │        │ lista arriba │
│ items + cant.    │ total         │        │ pago abajo   │
│ (scroll propio)  │ [Efectivo]   │        │ [confirmar]  │ ← sticky abajo
│                  │ [QR]         │        └──────────────┘
│                  │ ☐ pagos sep. │
│                  │ [confirmar]  │ ← fijo, nunca scrollea
└──────────────────┴──────────────┘
```

El panel de pago no scrollea: total, métodos y confirmar siempre visibles.

## Reglas de estado

| Situación | Comportamiento |
|---|---|
| Sin nada seleccionado | sección de pago **inerte** (fantasma), confirmar deshabilitado |
| El total cambia y el pago **no fue tocado** (sigue siendo la sugerencia) | recomputar la sugerencia **en silencio** |
| El total cambia y el pago **fue editado a mano** | **reset a la sugerencia nueva** + aviso sutil ("el monto cambió — revisa cómo se devuelve") |
| `sum(payments) != total` | confirmar deshabilitado (nunca enviar algo que el backend rebota) |

**La intención del usuario sobrevive solo si sigue siendo válida** — la elección manual hecha
contra un total viejo se descarta; mantenerla en silencio es cómo nacen los errores de caja.

## Sugerencias (prellenado)

| Caso | Sugerencia |
|---|---|
| POS (crear venta) | sin sugerencia de split: un tender con el total; el vendedor elige método |
| Devolución **total** | **espejo exacto** de los tenders originales (`details → payments[]`) |
| Devolución **parcial** | un solo tender: método dominante original por el monto completo; el vendedor ajusta o tilda "pagos separado" |

Nunca sugerencia proporcional (regla de diseño del backend).

## Reutilización

Mismo componente en POS y devoluciones: en ambos se envían montos **positivos** (el backend
niega el signo al guardar devoluciones). La única diferencia es la fuente del total y de la
sugerencia (carrito vs. `details`).

---

# ETAPA B — Escrituras (recién cuando la Etapa A esté aplicada)

## Paso 4 — POS: crear venta con `payments[]`

### Archivos backend para revisión

| Archivo | Qué revisar |
|---|---|
| `src/modules/Module.Sales/Application/UseCases/Sales/Create/CreateSaleDto.cs` | `payments[]` requerido; montos > 0; sin métodos duplicados; ❌ sin `paymentMethod` |
| `src/modules/Module.Sales/Application/UseCases/Sales/Create/CreateSale.cs` | valida `sum(payments) == total` (server-side) y persiste las líneas |
| `src/modules/Module.Sales/Application/UseCases/Sales/Create/CreateSaleErrors.cs` | error `PaymentAmountMismatch` (400) |
| `src/modules/Module.Sales/Domain/Sale.cs` | `AddPayment` (guarda de duplicados) |
| `src/modules/Module.Sales/Domain/SalePayment.cs` | la entidad de la línea |

### Contrato

```jsonc
POST /api/Sale
{
  "documentType": 0,
  "items": [ { "productVariantId": "…", "quantity": 2, "discountAmount": 0 } ],
  "payments": [ { "method": 0, "amount": 200.00 } ]        // caso inicial: un tender
}
```

Split (mismo contrato):

```jsonc
  "payments": [ { "method": 1, "amount": 85.00 }, { "method": 0, "amount": 50.00 } ]
```

### Errores a manejar

| HTTP | Código | Causa | UX sugerida |
|---|---|---|---|
| 400 | `PaymentAmountMismatch` | la suma no cierra (precio de catálogo cambió entre carga y confirmación) | re-validar carrito |
| 400 | validación DTO | payments vacío, monto ≤ 0, método duplicado | mensajes de formulario |

### Tareas del frontend

- [ ] Payload sin `paymentMethod`; total del carrito como `amount` del tender.
- [ ] Usar el **componente de pago compartido** (sección anterior): botones de método,
      checkbox "pagos separado", confirmar deshabilitado hasta cerrar la suma.

### Criterio de aceptación

- Venta con un método se crea y su detalle/lista muestra 1 línea.
- Venta split crea 2 líneas; el cierre muestra cada método con su monto real.

---

## Paso 5 — Devoluciones: elegir cómo se devuelve

> **Sin endpoint dedicado:** `GET /api/Sale/{id}/returnable` fue **eliminado del backend**.
> La pantalla de devolución se alimenta de **`GET /api/Sale/{id}/details`** (Paso 2), que ya
> trae todo lo necesario — incluida la sugerencia de reembolso.

### Mapeo de datos (details → pantalla de devolución)

| Necesito | En `GET /{id}/details` |
|---|---|
| Nº de venta que se manipula | `number` |
| Sugerencia de reembolso | `payments[]` |
| Ítems devolvibles | `items[]` → `id` (= `originalSaleItemId` del POST) |
| Cantidad devolvible por ítem | `items[].quantity − items[].returnedQuantity` |
| Precio/descuento para el monto exacto | `items[].unitPrice`, `items[].discountAmount` |
| ¿Se puede devolver? | `canReturn` (false si ya tiene devolución) · estado en `returnStatus` |

### Archivos backend para revisión

| Archivo | Qué revisar |
|---|---|
| `src/modules/Module.Sales/Application/UseCases/Sales/GetById/GetSaleDetails.cs` | fuente de datos (Paso 2) |
| `src/modules/Module.Sales/Application/UseCases/Sales/Return/CreateReturnDto.cs` | `payments[]` requerido; montos positivos; sin duplicados |
| `src/modules/Module.Sales/Application/UseCases/Sales/Return/CreateReturn.cs` | valida `sum == reembolso`; niega signos al persistir |
| `src/modules/Module.Sales/Application/UseCases/Sales/Return/ReturnErrors.cs` | error `RefundAmountMismatch` (400) |

### Contrato

```jsonc
POST /api/Sale/{id}/returns
{
  "items":    [ { "originalSaleItemId": "…", "quantity": 1 } ],   // = items[].id del details
  "payments": [ { "method": 0, "amount": 100.00 } ]              // positivos; SIEMPRE requerido
}
```

**Monto exacto a reembolsar:** `Σ (unitPrice − discountAmount) × quantityDevuelta` por ítem.

### Header de la pantalla (obligatorio)

- [ ] **`#number` visible y prominente** — es el documento que se está manipulando; sin él
      el vendedor no sabe qué venta está devolviendo.
- [ ] Layout **chunky** tipo detalle de venta (cards), no el listado viejo: fecha
      (`createdAt`), vendedor (`soldByName`), total (`totalAmount`) y chips de los tenders
      originales (`payments[]`).

### Errores a manejar

| HTTP | Código | Causa | UX sugerida |
|---|---|---|---|
| 400 | `RefundAmountMismatch` | `sum(payments) != reembolso` | volver al selector de tenders mostrando la diferencia |

### Tareas del frontend

- [ ] Alimentar la pantalla desde `details`; gating con `canReturn`.
- [ ] Mostrar reembolso exacto con `(unitPrice − discount) × qty`.
- [ ] Precargar la sugerencia desde `payments[]` (editable) — ver reglas del componente de pago.
- [ ] Permitir 1 o 2 métodos; confirmar deshabilitado hasta cerrar la suma.
- [ ] Enviar `payments[]` siempre.

### Criterio de aceptación

- Devolución total de venta split con espejo exacto → cierre netea a 0 por método.
- Reembolso en otro método (efectivo aunque pagó QR) → el cierre refleja el elegido.
- El número de venta está visible durante toda la operación.

---

# Fuera de alcance (fase 2)

- **Exchange (cambio de mercadería)** y **multi-devolución por venta**: pospuestos; ver
  `Documentation/cambio-de-mercaderia-para-frontend.md`.
- `GET /api/Sale/search-by-sku`, `GET /api/Sale/returns` y `GET /api/Sale/{id}/returnable`:
  **eliminados del backend**. El listado general cubre los dos primeros; la pantalla de
  devolución se alimenta de `GET /{id}/details` (Paso 5).
- **Drop de la columna DB `Sales.PaymentMethod`**: pospuesto hasta que todo esto esté
  estable en producción (hoy la API ya no la expone; la columna solo se mantiene poblada
  internamente).

---

# Checklist de deploy conjunto

1. [ ] Etapa A (pasos 1–3) aprobada y aplicada.
2. [ ] Etapa B (pasos 4–5) aprobada y aplicada.
3. [ ] Migración `20261122014653_AddSalePayments` aplicada en la DB de **producción**
       (local ya aplicada y verificada: montos 1:1 con las ventas existentes).
4. [ ] `appsettings.json` → connection strings de producción.
5. [ ] Deploy backend + frontend **en el mismo release**.
6. [ ] Smoke test: venta 1 método → venta split → devolución con elección → cierre → arqueo.
