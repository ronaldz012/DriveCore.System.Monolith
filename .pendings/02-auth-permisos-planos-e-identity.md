# Pendiente 02 — Simplificación total de autenticación/autorización (permisos planos + un rol por usuario + Microsoft Identity self-hosted)

> Estado: **diseño completo, aprobado** — listo para implementar.
> Prioridad: **magna**. Ventana: **fin de semana** (viernes desarrollar, sábado smoke en copia,
> domingo migrar producción — el sábado es el día de más ventas, no se toca producción).
> Migraciones: generar, NO aplicar salvo orden explícita del usuario.
> Decidido con el usuario en conversación (nov 2026). Fase 2 (proveedor) se decide al final.

---

## 1. El problema de hoy

- **Autorización**: RBAC anidado por sucursal — `UserBranchRole (usuario, sucursal, rol)` →
  `Role` → `RoleFeaturePermission (RoleId, FeatureKey, Permissions text[])`, doble gate por
  `Branch.AllowedFeatureKeys` y `Plan.AllowedFeatureKeys`. Tres capas para saber si un
  vendedor puede abrir el POS.
- **Doble mecanismo de autorización paralelo**: bypass por `UserType {Standard, TenantAdmin,
  Owner}` (admins saltan `RequireFeatureFilter`) + permisos por rol. Dos sistemas, dos filtros
  (`RequireFeatureFilter` + `RequireUserTypeFilter`), dos ramas en `BuildAsync`.
- **`TenantAdmin` es un nivel teórico**: no hay pagos implementados, no protege nada real.
- **Sin gestión de roles**: no existe endpoint para editar un rol; los templates del plan
  (`DefaultRolesTemplate` JSON) son la única fuente.
- **Autenticación**: Auth0 externo (JWKS + JWE + `ExternalAuthId`), JWT sin claims propios,
  dependencia de terceros para un sistema de pocos usuarios B2B.
- **Bug montado**: `SessionStateService.Invalidate()` **nunca se llama** — cambiar
  permisos/sucursales de un usuario tarda hasta 30 min en reflejarse.

## 2. Fase 1 — Autorización (permisos planos, un rol por usuario)

### 2.1 Modelo nuevo

```
Role:        Id, TenantId, Name, IsSystem, Permissions: List<string>   ← columna text[]
User:        + RoleId (UN rol por usuario, repetido en todas sus branches)
UserBranch:  PK (UserId, BranchId)                                       ← asignación independiente del rol
Permission:  NO es tabla — catálogo de constantes en el backend
Feature:     queda como tabla (catálogo para el menú/rutas del front)
```

### 2.2 Permisos como strings sueltos — formato `{feature}.{action}`

```csharp
public static class Permissions
{
    public const string ProductsRead    = "products.read";
    public const string UsersCreate     = "users.create";
    public const string BranchesCreate  = "branches.create";
    public const string RolesManage     = "roles.manage";
    public const string TenantsPlan     = "tenants.plan";
    // ...
    public record Definition(string Key, string DisplayName, string FeatureKey);
    public static readonly IReadOnlyList<Definition> All = [ /* ~30 */ ];
}
```

- **El estándar de facto** (Casbin, K8s): `resource.action`.
- **2 niveles, SIN capa de módulo**: los feature keys ya son únicos a nivel global
  ("products", "pos", "sales") — el prefijo de módulo sería redundancia. El módulo queda
  como **metadato de `Feature`** (menú del front + filtro Warehouse del
  `BranchFeatureKeysResolver`), nunca en el string. Colisión futura ("reports" en dos
  módulos) → se califica el feature key (`inv_reports`), no se agrega capa a todos.
  Se conservan los feature keys actuales → `AllowedFeatureKeys` y `[RequireFeature]` intactos.
- Display names en la constante, no en DB. Un endpoint serializa el catálogo para el front.
- Integridad: los use cases validan contra `Permissions.All` — sin FK que obligue a migrar
  el catálogo con cada acción nueva.

### 2.3 Filtro por plan y branch — SIN cambios de mecanismo

```
permiso "products.create"  →  feature key = parte antes del punto ("products")
visible en branch  ⟺  "products" ∈ Branch.AllowedFeatureKeys
```

`Plan.AllowedFeatureKeys` y `Branch.AllowedFeatureKeys` sobreviven tal cual. El armado de
sesión: permisos del rol (1 query) + branches asignadas (1 query) + intersect por feature key,
repetido por branch en el DTO (shape actual intacto → front no cambia).

### 2.4 UserType → bool `IsTenantOwner` — el enum muere entero

- **Columna `Users."IsTenantOwner" boolean`** reemplaza a `UserType {Standard, TenantAdmin,
  Owner}`. El enum solo servía para ordenar (`Type >= TenantAdmin`, el bypass); muerto el
  bypass, no queda jerarquía — queda un hecho binario de la cuenta.
- **Invisible a la autorización**: los permisos del Owner salen de su rol sistema, como
  cualquier usuario. `IsTenantOwner` SOLO alimenta invariantes de dominio:
  `DeleteUser` → no se puede borrar; `UpdateUser` → no se le cambia el rol.
- Es ancla de cuenta: cuando exista facturación es el dueño legal (como Shopify:
  el Owner no es un permiso, es quién paga).
- Backfill: `IsTenantOwner = ("Type" = 2)`; los `Type = 1` (TenantAdmin) → `false`
  + rol "Administrador" (punto 2.6).
- Muere: `ToggleUserType`, `CanDemoteToStandard()`, `RequireUserTypeFilter`,
  `[RequireUserType]`, rama admin de `BuildAsync`, wildcard `"*"`, columna `Type`.
- String descartado (2 valores no justifican parsing); enum de 2 valores descartado
  (invita a reintroducir niveles; si una jerarquía vuelve a hacer falta, el enum se
  reintroduce con una migración — camino inverso no).

### 2.5 Operaciones de administración = permisos como cualquier feature

```
users.read · users.create · users.update · users.password_reset
branches.create · branches.update
roles.read · roles.manage
tenants.plan
```

Controllers admin (`UserController`, `RoleController`, `BranchController`) pasan de
`[RequireUserType(TenantAdmin)]` a `[RequirePermission("users.create")]` etc. —
**un solo mecanismo de autorización en todo el sistema**.

### 2.6 Templates sembrados (responden la pregunta del "Supervisor")

| Template | Set | Equivalente hoy |
|---|---|---|
| Vendedor | pos/sales/closures | igual |
| Almacenero | receptions/transfers/products | igual |
| Supervisor | todo negocio | igual (hoy NO crea usuarios — paridad) |
| **Administrador** (nuevo) | negocio + users.*/branches.*/roles.*/tenants.plan | ex-TenantAdmin |
| **Owner** (sistema) | absolutamente todo | bypass actual |

"No falta una escala — la escala muere": quien quiera un supervisor que también dé de alta
personal, crea un rol custom. Sin código nuevo.

### 2.7 Guardas de negocio (nuevas, una línea c/u)

1. **Rol sistema no editable** (`IsSystem`): nadie le vacía los permisos al Owner.
2. **Anti-escalada**: al asignar rol en `CreateUser`/`UpdateUser`,
   `si rolDestino.Permisos ⊄ misPermisos → Error` (como Shopify staff).

## 3. Fase 2 — Autenticación (ASP.NET Core Identity self-hosted, cookies)

### 3.1 Decisiones de diseño

- **Identity SOLO autentica** (quién sos). La autorización (qué podés) es nuestra fase 1.
- `AuthUser : IdentityUser<Guid>` con `TenantId`, `Type`, `Status` — **el Id de Identity ES
  el Id del User actual** (una tabla, sin link).
- `AuthDbContext : IdentityDbContext<AuthUser, IdentityRole<Guid>, Guid>` — tablas
  `AspNetRoles`/`AspNetUserRoles` se crean y quedan **vacías para siempre** (no usamos
  el sistema de roles de Identity; nuestro vínculo es `User.RoleId`).
- **Cookies**: `Domain=.mitalla.net`, `Secure`, `HttpOnly`. app y api comparten registrable
  domain → same-site, sin SameSite=None ni `credentials: 'include'` obligatorio.
- Claims dentro de la cookie al `SignInAsync`: `UserId`, `TenantId` →
  `TenantMiddleware` pasa de 85 líneas (~20: lee 2 claims, setea
  `ITenantConnectionContext`). El middleware NO baja a policies: debe correr para toda
  request antes de resolver el DbContext (los `[AllowAnonymous]`/`[ApiKey]` las saltan).
- **CSRF (nuevo, obligatorio)**: con cookies aparece. Antiforgery de .NET o header
  `X-Requested-With` exigido en pipeline.
- **Status check antes del sign-in** (Identity no sabe de `User.Status`).
- Lockout: 5 intentos / 10 min. Password policy: defaults de Identity (8/mayúscula/dígito).
- **Muere**: Auth0 entero (JWKS, JWE, `SpaClientSecret`, `ExternalAuthId`, `Auth0Seeder`).
- **Migración de usuarios**: Auth0 no entrega hashes → seed de `AspNetUsers` SIN contraseña +
  **reseteo forzado en el primer login**. Reset del usuario: superadmin genera token desde
  el panel (`GeneratePasswordResetTokenAsync`). Reset del Owner: contactar a plataforma
  (hasta habilitar email).

### 3.2 Por qué self-hosted y no Clerk/Auth0

- Escala: pocos usuarios B2B por tenant, alta desde el panel propio — los proveedores
  justifican con B2C masivo, social, M2M. Aqui solo pagamos complejidad.
- Identity trae hashing, lockout, tokens, policies de fábrica — "manejar contraseñas"
  es en realidad configurar.
- Ventaja real: control total y cero dependencia. Transferencia de usuarios: pocos → un
  mensaje de "restablecé tu contraseña" y listo.

## 4. Migración de datos (AuthDbContext, `__EFMigrationsHistory_shared`)

### Migración 1 — crear lo nuevo + backfill (reversible)

```
ALTER Roles ADD Permissions text[]
ALTER Users ADD RoleId (nullable primero), IsTenantOwner bool DEFAULT false
CREATE UserBranch
INSERT Roles.Permissions   ← expandir RoleFeaturePermission: "{FeatureKey}.{action}"
INSERT UserBranch          ← SELECT DISTINCT UserId, BranchId FROM UserBranchRole
UPDATE Users.RoleId        ← regla: rol de la membership más reciente; conflictos
                               (roles distintos por sucursal) → reporte para revisar
UPDATE Users.IsTenantOwner ← true WHERE "Type" = 2
INSERT rol "Administrador" ← por tenant, asignado a los Type=TenantAdmin
INSERT rol "Owner" sistema ← por tenant, asignado a los Type=Owner
```

Validar strings contra catálogo; los que no mapeen → reporte (no fallo la migración).

### Migración 2 — código al modelo nuevo (tablas viejas siguen vivas, rollback = drop de lo nuevo)

### Migración 3 — cleanup final (solo con código nuevo corriendo)

```
DROP UserBranchRole, RoleFeaturePermission
DROP Feature.AvailableActions
ALTER Users.RoleId NOT NULL (si aplica)
DROP Users."Type"
```

## 5. Contratos externos que NO cambian

| Contrato | Estado |
|---|---|
| `GET /api/Auth/Me` shape | **idéntico** — permisos repetidos por branch (filtrados por `AllowedFeatureKeys`) hasta que el front pida el shape plano |
| `RequireFeatureFilter` → `RequirePermissionFilter` | misma mecánica, valida `"{feature}.{action}"`; los 64 atributos siguen |
| `ActorContext` + header `X-Branch-Id` | intacto — fuente pasa a ser `UserBranch` |
| Inventory / Sales / `TenantMiddleware` (conectividad) | **cero cambios** |
| `POST/PUT /api/User` (DTO `BranchRoles`) | mismo shape; se valida un solo RoleId |
| `POST /api/Role` (DTO de permisos) | **CAMBIA**: lista de strings — pantalla de roles del front se adapta |
| Login (fase 2) | **CAMBIA**: cookie, sin Bearer; interceptor simplifica; logout + pantalla de reseteo nuevas |

## 6. Bugs/deuda montados en el mismo pase

1. `SessionStateService.Invalidate()` nunca se llama → invocar en `UpdateUser` (y
   en todo lo que cambie permisos/sucursales de un usuario).
2. `AddRole` no valida actions contra el catálogo → con constantes, valida contra `Permissions.All`.
3. `CounterKeys.cs`: consts muertos por `ToName()` — limpiar si se toca Common (opcional).

## 7. Plan del fin de semana

| Bloque | Qué | Quién |
|---|---|---|
| **Viernes** | Fase 1 completa: constantes, entidades, config, migración 1 + backfill, `BuildAsync` unificado, `CreateUser`/`UpdateUser`/`AddRole`/seeders, `RequirePermissionFilter`, guardas, fix `Invalidate`, ~14 tests reescritos | agente |
| **Sábado (día de ventas — SOLO copia)** | Usuario aplica migración 1 en `copy_talla_oct_2`, smoke de fase 1; agente arranca fase 2: `AuthUser`, `AuthDbContext : IdentityDbContext`, cookies+claims+antiforgery, login/logout/reset, muerte de Auth0, `TenantMiddleware` reducido | ambos |
| **Domingo AM (ventana de update)** | Aplicar migraciones en producción, deploy, smoke real, frontend (login cookie, interceptor sin Bearer, logout, reseteo, pantalla de roles) | usuario + agente corrige |

Rollback: las tablas viejas viven hasta migración 3; Auth0 no se borra del dashboard hasta
confirmar login nuevo en producción.

## 8. Estimación

| Tramo | Horas enfocadas |
|---|---|
| Fase 1: modelo + migración 1 + backfill | 3–4 h |
| Fase 1: switch enforcement + use cases + seeders + guardas + tests | 4–5 h |
| Fase 2: Identity + cookies + endpoints + antiforgery + muerte de Auth0 | 4–5 h |
| Fase 2: TenantMiddleware reducido + migración de usuarios + smoke | 2–3 h |
| **Backend total** | **~14–17 h** |
| Frontend (login, interceptor, logout, reseteo, pantalla de roles) | ~1 día |
| Migraciones aplicadas por el usuario | 3 ventanas |

**Veredicto**: es fin de semana completo punta a punta, con el sábado blindado para
producción. NO intentar en una sola noche.

## 9. Fuera de alcance (documentado)

- Facturación/suscripción real (`tenants.plan` queda de placeholder de permiso).
- Gate por **acción** (hoy el gate plan/branch es por feature entera; si se necesita apagar
  una acción puntual por branch, pasaría a `AllowedPermissionIds` — no asumir para siempre).
- Recepción parcial de permisos "por branch" (modelo granular `UserBranchPermission`) —
  descartado: es lo que justamente se elimina.
- Clerk/Auth0 re-evaluación si el sistema crece a B2C masivo.
- Email para reseteo de contraseñas (hasta entonces: token generado por el admin del tenant;
  Owner: contactar plataforma).
