# Auth Module — Multi-Tenant RBAC

## DbContext & Connection

| Context | Schema | Connection string | Migrations table |
|---------|--------|------------------|-----------------|
| `AuthDbContext` (Module.Auth) | `public` | `DefaultConnection` | `__EFMigrationsHistory_shared` |
| `AppDbContext` (Sales + Inventory) | `tenant_db` (per-tenant) | `TenantConnection` (Search Path=tenant_db) | `__EFMigrationsHistory` |

Auth es global (no multi-tenant). Los datos de tenant, usuarios, roles, branches y features están en `public`.  
AppDbContext es multi-tenant por schema: cada tenant tiene su propio schema `tenant_db` con sus tablas de Sales e Inventory.

---

## Database Tables

### TenantDataBase
| Column | Type | Notes |
|--------|------|-------|
| Id | GUID PK | |
| Name | string | "erp_db" |
| Description | string | |
| Schema | string | "tenant_db" |

### Tenant
| Column | Type | FK | Notes |
|--------|------|----|-------|
| Id | GUID PK | | |
| DisplayName | string | | Unique per tenant |
| IsActive | bool | | |
| OwnerId | GUID | → User.Id | |
| DataBaseId | GUID | → TenantDataBase.Id | |
| PlanId | GUID | → Plan.Id | |
| CreatedAt | datetime | | |

### Plan
| Column | Type | Notes |
|--------|------|-------|
| Id | GUID PK | |
| Name | string | "Basic" |
| Description | string | |
| Price | decimal | 150 |
| MaxUsers | int | 5 |
| MaxBranches | int | 3 |
| MaxExtraRoles | int | 1 |
| AllowedFeatureKeys | `List<string>` | JSON column |
| DefaultRolesTemplate | `List<DefaultRoleTemplate>` | JSON column (owned) |

### Feature
| Column | Type | Notes |
|--------|------|-------|
| Key | string PK (100) | e.g. "products" — source of truth |
| Route | string | Frontend route |
| DisplayName | string | |
| Description | string | |
| Icon | string | Material icon |
| Module | enum (int) | `Inventory=0`, `Sales=1` |
| IsMenu | bool | Show in sidebar? |
| CreatedAt | datetime | |
| UpdatedAt | datetime? | |
| AvailableActions | `List<FeatureAction>` | JSON column: `[{Key, DisplayName, Description}]` |

### User
| Column | Type | Notes |
|--------|------|-------|
| Id | GUID PK | |
| TenantId | GUID | Global tenant filter |
| Username | string (100) | |
| Email | string (100)? | Unique index, filter not null |
| FirstName | string (100) | |
| LastName | string (100) | |
| Ci | string (15) | |
| Nationality | string | |
| BirthDate | datetime | |
| PasswordHash | string | BCrypt |
| Type | enum (int) | `Standard=0`, `TenantAdmin=1`, `Owner=2` |
| IsAdmin | computed | `Type is 1 or 2` |
| Status | enum (int) | `PendingPasswordSetup=1`, `Ready=2` |
| GoogleId | string? | |
| AuthProvider | enum (int) | `Local=0`, `Google=1` |
| ExternalAuthId | string? | |
| IsActive | bool | |
| LastActive | datetime | |
| CreatedAt | datetime | |
| UpdatedAt | datetime? | |
| DeletedAt | datetime? | Soft delete |
| CreatedBy | int | |
| UpdatedBy | int? | |
| DeletedBy | int? | |

### Role
| Column | Type | Notes |
|--------|------|-------|
| Id | GUID PK | |
| TenantId | GUID | |
| Name | string | Unique per tenant (e.g. Vendedor) |
| Description | string | |
| Public | bool | |
| CreatedAt | datetime | |
| DeletedAt | datetime? | |
| CreatedBy | int | |
| DeletedBy | int? | |

### Branch
| Column | Type | Notes |
|--------|------|-------|
| Id | GUID PK | |
| TenantId | GUID | |
| Name | string | e.g. "Main Branch" |
| Place | string | |
| PhoneNumber | string | |
| IsActive | bool | |
| BranchCode | string | |
| CreatedAt | datetime | |

### UserBranchRole (junction)
| Column | Type | Notes |
|--------|------|-------|
| UserId | GUID PK | → User.Id |
| BranchId | GUID PK | → Branch.Id |
| RoleId | GUID PK | → Role.Id |
| TenantId | GUID | |
| CreatedAt | datetime | |
| CreatedBy | int | |
| DeletedAt | datetime? | |
| DeletedBy | int? | |

Non-admin users have exactly 1 Role per Branch. No UserBranchRole = no branch access.  
Admin users (Owner, TenantAdmin) bypass this entirely.

### RoleFeaturePermission
| Column | Type | Notes |
|--------|------|-------|
| Id | GUID PK | |
| RoleId | GUID | → Role.Id |
| FeatureKey | string | → Feature.Key |
| Permissions | `List<string>` | Stored actions (e.g. `["read","create"]`) |
| TenantId | GUID | |
| CreatedAt | datetime | |
| UpdatedAt | datetime? | |
| UpdatedBy | int? | |

Unique index on (RoleId, FeatureKey).

### EmailVerificationCode
| Column | Type | Notes |
|--------|------|-------|
| Id | int PK | |
| UserId | GUID | → User.Id |
| TenantId | GUID | |
| Code | string | GUID N |
| Email | string | |
| SentAt | datetime | |
| ExpiresAt | datetime | 48h for account setup |
| IsUsed | bool | |
| Attempts | int | |
| Purpose | enum | `AccountVerification`, `PasswordReset`, `EmailChange` |

---

## Seeded Data

### Seed order
1. **TenantDataBaseSeeder** (Order 1): Creates "erp_db" with schema "tenant_db"
2. **FeatureSeeder** (Order 2): Creates feature catalog
3. **PlanSeeder** (Order 3): Creates Basic plan with role templates
4. **TenantSeeder** (Order 4): Creates default tenant, admin user, 2 branches, 3 roles

### Features (6 total)

| Key | Module | Menu | Actions |
|-----|--------|------|---------|
| `products` | Inventory | ✅ | read, create, update, delete |
| `transfers` | Inventory | ✅ | read, create, update, delete |
| `receptions` | Inventory | ✅ | read, create, update, delete |
| `pos` | Sales | ✅ | read (POS access), create (Process sale), update (Modify cart) |
| `sales` | Sales | ✅ | read (View), readAll (all sellers' stats), margin (UnitPrice/UnitCost) |
| `closures` | Sales | ✅ | read (View), export |

### Plan "Basic"
- Price: 150, Max Users: 5, Max Branches: 3, Max Extra Roles: 1
- Allowed: products, receptions, transfers, pos, sales, closures

#### Role "Vendedor"
- `products:read`, `transfers:read`, `pos:read/create/update`, `sales:read`, `closures:read`

#### Role "Almacenero"
- `products:read/create/update`, `receptions:read/create/update/delete`, `transfers:read/create/update/delete`

#### Role "Supervisor"
- All features with full CRUD + `sales:read/readAll/margin` + `closures:read/create/update`

### Default Tenant
- DisplayName: "default", Owner: admin@drivecore.com / admin / "1234"
- Branches: "Main Branch", "Secondary Branch"
- Owner user type bypasses all permission checks (no UserBranchRole needed)

---

## Auth Flow

### JWT Claims
| Claim | Source |
|-------|--------|
| `ClaimTypes.NameIdentifier` | User.Id |
| `tenantId` | User.TenantId |
| `ClaimTypes.Name` | `FirstName + " " + LastName` |
| `username` | User.Username |
| `user_type` | User.Type (int) |

### TenantMiddleware (pipeline)
Resolves tenant from JWT `tenantId` claim → calls `ITenantDatabaseResolver.GetTenantDatabaseInfo(tenantId)` → sets `ITenantConnectionContext` (TenantId, Schema, DatabaseName).

### CurrentUserService
Resolves from `IHttpContextAccessor` + `X-Branch-Id` header:
- `BranchId` = first value from `X-Branch-Id`
- `BranchIds` = all values from `X-Branch-Id` (multi-branch support)
- `IsAdmin` = true when UserType is 1 (TenantAdmin) or 2 (Owner)

### RequireFeatureFilter
- Skip check if user `IsAdmin`
- Validate `X-Branch-Id` header (single vs multi-branch endpoints)
- Check branch access via `ISessionStateService`
- Check feature + permission via session modules
- Returns 403 with details on failure

### SessionStateService
- Caches user permissions per tenant
- `GetOrBuildAsync(userId, tenantId, userType)` builds session from RoleFeaturePermissions
- `InvalidateTenant(tenantId)` expires all cache entries for a tenant via CancellationTokenSource

---

## Key Interfaces (in Common)

| Interface | Implementation | Purpose |
|-----------|---------------|---------|
| `ICurrentUser` | `CurrentUserService` | Current request user context |
| `ITenantConnectionContext` | Per-request scoped | Tenant connection info |
| `ITenantDatabaseResolver` | `TenantDatabaseResolverService` | Resolve tenant by ID/display name |
| `IBranchService` | `BranchService` | Batch branch lookup (name by ID) |
| `IUserIntegrationService` | `UserIntegrationService` | Batch user lookup (name by ID) |
| `ISessionStateService` | `SessionStateService` | Cached user permissions |
| `ITokenGenerator` | `JwtTokenGenerator` | JWT creation |
| `IGoogleTokenValidator` | `GoogleTokenValidator` | Google OAuth validation |
| `IEmailService` | In Common | Email sending |

---

## ICurrentUser API
```csharp
Guid UserId { get; }
Guid TenantId { get; }
Guid BranchId { get; }           // First from X-Branch-Id header
IReadOnlyList<Guid> BranchIds { get; }  // All from X-Branch-Id
string FullName { get; }
string Username { get; }
int UserType { get; }            // 0=Standard, 1=TenantAdmin, 2=Owner
bool IsAdmin { get; }            // UserType is 1 or 2
bool IsAuthenticated { get; }
string? Token { get; }
```

---

## ITenantConnectionContext API
```csharp
string? Schema { get; set; }       // "tenant_db"
Guid? TenantId { get; set; }
string? DatabaseName { get; set; } // "erp_db"
DbConnection Connection { get; }
Task EnsureOpenAsync();
```

---

## Validation Rules
- `RoleFeaturePermission.Permissions` entries must exist in `Feature.AvailableActions` (validated in PlanSeeder)
- Non-admin users must have a `UserBranchRole` to access a branch
- `Email` unique index with NULLS NOT DISTINCT filter
- Role name unique per tenant
- One open CashRegisterClosure per branch (filtered index)
