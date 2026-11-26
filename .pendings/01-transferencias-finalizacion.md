# Pendiente 01 — Finalizar transferencias (modelo Shopify: Draft → Committed → Completed)

> Estado: **análisis completo, aprobado** — listo para implementar.
> Prioridad: **media**.
> Referencia de mercado: Shopify Inventory Transfers (verificado en docs oficiales, nov 2026).
> Decisiones tomadas con el usuario en conversación. No aplicar migraciones salvo orden explícita.

---

## 1. El problema de hoy

- El flujo actual es `Pending → (accept) Completed / Rejected / Cancelled`.
- El stock **no se toca** al crear la solicitud: mientras está `Pending` (días), el origen
  sigue vendiendo las unidades prometidas y el destino no ve nada ("incoming" no existe).
- `TransferStatus.Transit` es un **valor huérfano**: nada lo asigna jamás.

## 2. El modelo nuevo (decisión: modelo de descuento físico, NO derivado en queries)

**Por qué**: al descontar físicamente al comprometer, **ninguna query de stock cambia** —
detalles de producto, listados y POS siguen leyendo `BranchInventory.Stock` tal cual.
El bucket de tránsito **son los ítems de la transferencia comprometida** (sin tabla nueva).
Nunca doble-contar: o se descuenta físicamente, o se resta en queries — aquí se descuenta.

### Estados y slots del enum (los ints NO cambian → sin migración de estado, front sin renumerar)

| Slot | HOY | NUEVO | Significado |
|---|---|---|---|
| 0 | Pending | **Draft** | solicitud creada, nada comprometido, stock sigue vendible en origen |
| 1 | Transit (muerto) | **Committed** | despachada: stock salió del origen, vive en los ítems de la transferencia |
| 2 | Completed | Completed | recibida por el destino |
| 3 | Rejected | Rejected | resuelta sin recibir (desde Draft: nada; con devolución/baja ver disposiciones) |
| 4 | Cancelled | Cancelled | cancelada por el origen (desde Draft: nada; desde Committed: con resolución) |
| 5 | — | **Lost** (nuevo, append) | mercadería perdida/dañada en tránsito: no está en ninguna sucursal |

Los datos existentes (todos 0/2/3/4) quedan válidos sin migración.

### Máquina de estados con actores

```
                    ┌──────────────────────────────────────────────┐
                    │                                              │
  origen: crear     │ destino: receive directo (opcional,          │
  ───────────►  Draft ────────────────────────────────────────► Completed
                │  │    (dos movimientos atómicos, sin pasar
                │  │     por Committed — el "Resolve" actual)
                │  │
                │  │ origen: commit (despachar)
                │  ▼
                │ Committed ── destino: receive ──► Completed (+qty destino)
                │  │  │
                │  │  │ destino u origen: return ──► Rejected/Cancelled (+qty origen)
                │  │  │ destino u origen: write-off ─► Lost (sin movimiento)
                │  │
                │ destino: reject (rechaza la SOLICITUD, nada salió)
                ▼  ▼
              Rejected / Cancelled (sin tocar stock)
```

## 3. Disposiciones — todo tránsito termina en 1 de 3 lugares (exhaustivo)

| Disposición | Stock | Movimiento | Caso físico |
|---|---|---|---|
| **Receive** | destino +qty | `TransferIn` (el `TransferOut` ya existía del commit) | llegó bien |
| **Return** | origen +qty | `TransferReturn` (nuevo type, append al enum) | dañada y vuelve / origen aborta el despacho |
| **Write-off** | nadie | **ninguno** — la baja es la *ausencia* del In/Return, con estado `Lost` + notas | dañada y se descarta / perdida / nunca salió de verdad |

Casos cubiertos:
1. **Llegó dañada** → return (vuelve) o write-off (se descarta).
2. **Nunca llegó** → el **origen aborta** el Committed: return si nunca despachó de verdad,
   write-off si se perdió. (Sin abort del origen, el destino queda con tarea eterna.)
3. **Cualquier otra** (parcial, error de sucursal, robo) → las tres disposiciones cubren
   todo lo físicamente posible.

Notas de diseño:
- **Reject bifurcado por estado**: en Draft = rechazar la solicitud (nada se mueve);
  en Committed la acción ya no se llama reject — elige disposición (return / write-off).
- **La devolución es un ingreso, no un rollback**: `+qty` sobre el stock *actual* del origen;
  el ledger refleja el momento de la devolución, no restaura un snapshot.
- **Write-off sin movimiento nuevo**: el `TransferOut` del commit ya registró la salida;
  trazabilidad vía `ReferenceId` → transferencia en `Lost` con nota. Invariante limpia:
  todo movimiento tiene efecto de stock (nada de movimientos qty 0).
- **Receive directo** (Draft → Completed): el "Resolve" actual sobrevive como camino corto
  (despacho+recepción atómicos, dos movimientos en una transacción).
- **Completed es terminal**: un error post-recepción se corrige con transferencia inversa.

## 4. Movimientos

| Momento | Movimiento | Sucursal |
|---|---|---|
| Commit | `TransferOut` (−qty) | origen |
| Receive (final) | `TransferIn` (+qty) | destino |
| Receive directo | `TransferOut` + `TransferIn` atómicos | ambas |
| Return | `TransferReturn` (+qty) | origen |
| Write-off | — | — (estado `Lost` + notas) |

`MovementType`: agregar `TransferReturn` **al final** del enum (después de `ReceptionRevert`),
sin renumerar. Primer feature con ledger en dos fases — correcto: el proceso físico es en dos fases.

## 5. Decisiones conscientes (documentadas, no bugs)

- **AVCO durante tránsito — DECISIÓN FINAL: incluir el tránsito en la base del cálculo**
  (no ignorar, como se evaluó primero). `AverageCost` es global por variante y recalcula
  sobre `Σ BranchInventories.Stock` en exactamente **3 sitios**: `Reception.cs:69`
  (confirmar recepción), `RevertStockReception.cs:79` (revertir) e
  `InventoryIntegrationService.cs:106` (devolución de venta). Con Committed, las unidades
  en tránsito no están en ninguna `BranchInventory` → quedaban fuera de la base y una
  recepción durante el tránsito recalculaba mal (ej.: 2 en tránsito @10, recepción 10 @12
  → promedio 12 en vez de 11.67).
  Contablemente las unidades en tránsito siguen siendo inventario propio → deben estar
  en el pool del promedio.
  **Fix acordado** (descartado el approach derivado — arreglaba AVCO gratis pero imponía
  restar transferencias en ~8 lecturas de stock para siempre):
  - `RegisterPurchase`/`RevertPurchase` reciben `int stockInTransit = 0` y lo suman a la
    base (el dominio recibe un int, no conoce transferencias — pureza intacta).
  - Los 3 callers hacen 1 query (total comprometido de esas variantes en transferencias
    `Committed`) y lo pasan. 1 query por operación de compra/devolución — raras, no por lectura.
  - Con esto: commit/receive/return **no** recalculan (las unidades no cambian de pool) y
    **Lost tampoco** (sacar unidades al costo promedio no cambia el promedio de las que quedan).
  - Discard documentado: costo promedio **por sucursal** (pool por `BranchInventory`) —
    correcto pero obliga a mover valor entre pools en cada transferencia y a rehacer el
    reporting de márgenes. Rediseño grande, fuera de la mesa.
- **Concurrencia**: transición validada **dentro de la transacción**
  (`Status == Committed`) → doble resolución simultánea = segundo recibe 400, cero
  doble devolución. El fix de "validación fuera de la transacción" cubre esto.
- **Sin recepción parcial en fase 1**: resolución por transferencia completa.
  El marco generaliza sin rediseño: fase 2 = dividir la disposición por ítem
  (`QuantityReceived` por ítem, como Shopify). Workaround documentado: resolver completo
  + `Adjustment` con nota.

## 6. Bugs existentes a montar en el mismo pase (deuda, no del modelo nuevo)

1. `CreateTransferDto.QuantityRequested` sin validación: qty `<= 0` pasa los chequeos
   (`Stock < -5` es falso) y deja la transferencia **atorcada** — al aceptar,
   `StockMovement.CreateTransfer` lanza → 500 para siempre.
2. **Ítems duplicados no se suman**: dos líneas del mismo variant con 10 c/u y stock 10
   pasan el chequeo por línea → al aceptar el origen queda en −10.
3. **Validación de stock fuera de la transacción** en `ResolveStockTransfer` (líneas 39-49
   vs `BeginTransactionAsync` en 51) + `AddQuantity` resta sin validar → venta concurrente
   deja stock negativo silencioso.
4. Menor: `Cancel` setea `UpdatedBy/UpdatedAt` pero no `UpdatedByName`.

## 7. Plan de implementación (orden)

### Backend
1. **Dominio** (`StockTransfer.cs`)
   - Renombrar `Pending → Draft`, `Transit → Committed`, agregar `Lost`.
   - Métodos: `Commit(userId, userName, notes?)` (Draft→Committed),
     `Receive(...)` (Committed→Completed / directo),
     `Return(...)` (+qty origen, `TransferReturn`),
     `WriteOff(...)` (→`Lost`), `Reject`/`Cancel` bifurcados por estado.
   - `StockMovement`: factory `CreateTransferReturn(...)` (+qty en origen, `ReferenceId` = transfer).
   - `MovementType`: append `TransferReturn`.
2. **Use cases** (`Module.Inventory/Application/UseCases/Transfers/`)
   - `CreateStockTransfer`: validar qty `> 0`, lista no vacía, **sumar duplicados antes
     de validar**, seguir sin tocar stock (Draft).
   - **Nuevo** `CommitStockTransfer` (origen): re-validar stock **dentro de la tx**,
     `−qty` origen + `TransferOut` por ítem.
   - **Nuevo** `ReceiveStockTransfer` (destino): desde Committed (`+qty` + `TransferIn`)
     o desde Draft (directo: ambos movimientos atómicos).
   - **Nuevo** `ResolveDisposition` o similar (destino u origen): `return` / `write-off`
     sobre Committed; también abort del origen.
   - `CancelStockTransfer`: Draft (nada) / Committed (elige return o write-off).
   - Eliminar `ResolveStockTransfer` (absorbido por receive/disposición).
3. **Controller** (`StockTransferController.cs`)
   - `POST /{id}/commit` · `POST /{id}/receive` · `POST /{id}/reject` ·
     `PATCH /{id}/cancel` (+ body de disposición donde aplique).
   - Permisos: commit/cancel = `transfers:create|delete` en **origen**;
     receive/reject = `transfers:update` en **destino** (validar por `From/ToBranchId`
     contra `ctx.BranchId`, como hoy).
4. **Auditoría**: `CommittedAt` + `CommittedByUserId` (nullable) → **migración pequeña,
   solo generar, NO aplicar** salvo orden explícita.
5. `BranchHasPendingTransfers`: cubrir `Draft || Committed` (renombre del check actual
   que ya incluía el slot 1). Sigue bloqueando desactivar sucursal con transferencias abiertas.
6. Seeder: siembra estado 0 — compatible (label `Draft`); ajustar nombre en code solo si se toca.
7. **AVCO + tránsito**: `RegisterPurchase`/`RevertPurchase` con `int stockInTransit = 0`
   en `ProductVariant.cs`; los 3 callers (`Reception.cs` confirm, `RevertStockReception.cs`,
   `InventoryIntegrationService.ReturnStock`) resuelven el total comprometido con 1 query
   (`StockTransferItems` de transferencias `Committed` agrupado por variante) y lo pasan.
   Helper compartido para esa query (lo reutilizan commit/receive para validaciones).

### Tests (actualizar existentes; nuevos solo para los bugs 1-3)
- `CreateStockTransferTests`: qty inválida, duplicados sumados.
- `ResolveStockTransferTests` → reescribir a `Commit/Receive/Return/WriteOff`.
- Flujos nuevos: commit descuenta, receive directo, return devuelve, lost no crea stock fantasma,
  doble resolución → 400.

### Frontend (doc para el front, formato del doc de pagos)
- Labels: 0 Draft (ex-"Pendiente"), 1 Committed ("En tránsito"), 5 Lost ("Perdida").
  **Los ints no cambian** — solo labels + chips nuevos.
- Acciones visibles según estado y dirección:
  - Draft: origen [Despachar, Cancelar] · destino [Recibir directo, Rechazar solicitud]
  - Committed: origen [Abortar (devolver/dar de baja)] · destino [Recibir, Devolver, Dar de baja]
  - Completed/Rejected/Cancelled/Lost: solo lectura.
- Transfer detail: mostrar movimientos (out/in/return) + notas de la disposición.
- Fase 2 (fuera): recepción parcial por ítem, shipments múltiples.

## 8. Estimación (yo controlo backend + frontend, deploy conjunto)

| Tramo | Horas enfocadas |
|---|---|
| Dominio + movimientos | 2–3 h |
| Use cases (commit/receive/disposición + fixes de validación) | 6–7 h |
| AVCO + tránsito (parámetro en dominio + query en 3 callers) | 1–2 h |
| Controller + DI + migración (generada) | 1–2 h |
| Tests (reescritura + casos de bugs + AVCO con tránsito) | 3–4 h |
| **Backend total** | **~14–17 h** |
| Frontend (labels, acciones por estado, UI de disposición, doc) | 1 día |
| Smoke (draft→commit→receive, return, lost, venta durante tránsito) | 1–2 h |

**Veredicto**: NO es una noche hot. Son **2–3 noches hot de backend + 1 día de front**,
o **un fin de semana completo** punta a punta. Una sola noche solo alcanzaría para el
slice mínimo (Commit + Receive + Return, sin Lost ni abort del origen) — no recomendado:
dejaría el caso 2 (nunca llegó) sin solución.

## 9. Fuera de alcance (fase 2, documentado)

- Recepción **parcial** por ítem (`QuantityReceived`, disposición por ítem como Shopify).
- Shipment múltiples.
- Vista "en tránsito" por variante en la ficha de producto (display-only, opcional).
