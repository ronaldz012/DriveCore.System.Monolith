using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.Auth.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesMarginPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data-only migration: introduces the field-level 'margin' permission
            // used by field gates such as SaleController.GetSaleDetail
            // ("sales" + "margin" reveals UnitPrice/UnitCost).
            //
            // Every statement is idempotent (guarded with NOT @>) because:
            // - this migration runs BEFORE seeders on a fresh database, and the
            //   updated FeatureSeeder/PlanSeeder may have already added the action;
            // - 'dotnet ef database update' may be re-run against a partially
            //   migrated copy during rehearsal.

            // 1. Feature catalog: add the 'margin' action to the 'sales' feature.
            //    AvailableActions is a jsonb column: [ { Key, Description, DisplayName } ].
            migrationBuilder.Sql(@"
                UPDATE ""Features""
                SET ""AvailableActions"" = ""AvailableActions"" ||
                    '{""Key"":""margin"",""Description"":""Permite ver el precio y costo con margen en el detalle de ventas."",""DisplayName"":""Ver Márgenes""}'::jsonb
                WHERE ""Key"" = 'sales'
                  AND NOT (""AvailableActions"" @> '[{""Key"":""margin""}]'::jsonb);");

            // 2. Existing roles. Role templates are system roles (fixed names,
            //    non-renamable), so a name-based bulk update is deterministic.
            //
            //    Supervisor: append 'margin' to the existing 'sales' permission row.
            //    (Permissions is a text[] column in public.RoleFeaturePermissions.)
            migrationBuilder.Sql(@"
                UPDATE ""RoleFeaturePermissions"" AS rfp
                SET ""Permissions"" = array_append(""Permissions"", 'margin')
                FROM ""Roles"" AS r
                WHERE rfp.""RoleId"" = r.""Id""
                  AND r.""Name"" = 'Supervisor'
                  AND r.""DeletedAt"" IS NULL
                  AND rfp.""FeatureKey"" = 'sales'
                  AND NOT ('margin' = ANY(rfp.""Permissions""));");

            //    Vendedor: grant 'sales:read'. Some roles have no 'sales' row at all
            //    (verified on production data): insert it; update the rest to ensure
            //    the action is present. 'margin' is never granted to Vendedor.
            migrationBuilder.Sql(@"
                INSERT INTO ""RoleFeaturePermissions""
                    (""Id"", ""RoleId"", ""FeatureKey"", ""Permissions"", ""TenantId"",
                     ""CreatedAt"", ""CreatedBy"", ""CreatedByName"")
                SELECT gen_random_uuid(), r.""Id"", 'sales', ARRAY['read']::text[], r.""TenantId"",
                       now(), r.""CreatedBy"", r.""CreatedByName""
                FROM ""Roles"" AS r
                WHERE r.""Name"" = 'Vendedor'
                  AND r.""DeletedAt"" IS NULL
                  AND NOT EXISTS (
                    SELECT 1 FROM ""RoleFeaturePermissions"" AS x
                    WHERE x.""RoleId"" = r.""Id"" AND x.""FeatureKey"" = 'sales');");

            migrationBuilder.Sql(@"
                UPDATE ""RoleFeaturePermissions"" AS rfp
                SET ""Permissions"" = array_append(""Permissions"", 'read')
                FROM ""Roles"" AS r
                WHERE rfp.""RoleId"" = r.""Id""
                  AND r.""Name"" = 'Vendedor'
                  AND r.""DeletedAt"" IS NULL
                  AND rfp.""FeatureKey"" = 'sales'
                  AND NOT ('read' = ANY(rfp.""Permissions""));");

            // 3. Plan "Basic": fix the persisted role template so tenants created
            //    from now on get correct defaults (CreateTenant reads this JSON).
            //    The array layout is fixed by PlanSeeder: [0]=Vendedor, [1]=Almacenero,
            //    [2]=Supervisor; the 'sales' entry is Permissions[4]
            //    (products, transfers, receptions, pos, sales, closures).
            //
            //    Vendedor [0]: append { Actions: ["read"], FeatureKey: "sales" } to
            //    its Permissions when there is no 'sales' entry yet.
            migrationBuilder.Sql(@"
                UPDATE ""Plans""
                SET ""DefaultRolesTemplate"" = jsonb_set(
                    ""DefaultRolesTemplate"",
                    '{0,Permissions}',
                    ""DefaultRolesTemplate"" #> '{0,Permissions}' ||
                        '[{""Actions"":[""read""],""FeatureKey"":""sales""}]'::jsonb)
                WHERE ""Name"" = 'Basic'
                  AND ""DefaultRolesTemplate"" @> '[{""Name"":""Vendedor""}]'::jsonb
                  AND NOT jsonb_path_exists(
                    ""DefaultRolesTemplate"",
                    '$[0].Permissions[*] ? (@.""FeatureKey"" == ""sales"")');");

            //    Supervisor [2]: extend the 'sales' entry actions to ["read","margin"].
            migrationBuilder.Sql(@"
                UPDATE ""Plans""
                SET ""DefaultRolesTemplate"" = jsonb_set(
                    ""DefaultRolesTemplate"",
                    '{2,Permissions,4,Actions}',
                    '[""read"",""margin""]'::jsonb)
                WHERE ""Name"" = 'Basic'
                  AND ""DefaultRolesTemplate"" @> '[{""Name"":""Supervisor""}]'::jsonb
                  AND ""DefaultRolesTemplate"" #> '{2,Permissions,4,FeatureKey}' = '""sales""'::jsonb
                  AND NOT (""DefaultRolesTemplate"" #> '{2,Permissions,4,Actions}' @> '""margin""'::jsonb);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Best-effort reversal of the data changes made by Up. On a fresh
            // database the seeders ran after this migration, so reverting also
            // removes what the seeders added (re-running the app will NOT
            // re-seed them because those seeders are guarded by Any()).

            // 3. Plan "Basic": restore former template values.
            migrationBuilder.Sql(@"
                UPDATE ""Plans""
                SET ""DefaultRolesTemplate"" = jsonb_set(
                    ""DefaultRolesTemplate"",
                    '{2,Permissions,4,Actions}',
                    '[""read""]'::jsonb)
                WHERE ""Name"" = 'Basic'
                  AND ""DefaultRolesTemplate"" #> '{2,Permissions,4,FeatureKey}' = '""sales""'::jsonb;");

            migrationBuilder.Sql(@"
                UPDATE ""Plans"" AS p
                SET ""DefaultRolesTemplate"" = jsonb_set(
                    p.""DefaultRolesTemplate"",
                    '{0,Permissions}',
                    (
                        SELECT COALESCE(jsonb_agg(e), '[]'::jsonb)
                        FROM jsonb_array_elements(p.""DefaultRolesTemplate"" #> '{0,Permissions}') AS e
                        WHERE e ->> 'FeatureKey' <> 'sales'
                    ))
                WHERE ""Name"" = 'Basic'
                  AND jsonb_path_exists(
                    p.""DefaultRolesTemplate"",
                    '$[0].Permissions[*] ? (@.""FeatureKey"" == ""sales"")');");

            // 2. Existing roles.
            migrationBuilder.Sql(@"
                DELETE FROM ""RoleFeaturePermissions"" AS rfp
                USING ""Roles"" AS r
                WHERE rfp.""RoleId"" = r.""Id""
                  AND r.""Name"" = 'Vendedor'
                  AND rfp.""FeatureKey"" = 'sales';");

            migrationBuilder.Sql(@"
                UPDATE ""RoleFeaturePermissions"" AS rfp
                SET ""Permissions"" = array_remove(rfp.""Permissions"", 'margin')
                FROM ""Roles"" AS r
                WHERE rfp.""RoleId"" = r.""Id""
                  AND r.""Name"" = 'Supervisor'
                  AND rfp.""FeatureKey"" = 'sales';");

            // 1. Feature catalog: drop the 'margin' action (rebuild the array).
            migrationBuilder.Sql(@"
                UPDATE ""Features"" AS f
                SET ""AvailableActions"" = (
                    SELECT COALESCE(jsonb_agg(e), '[]'::jsonb)
                    FROM jsonb_array_elements(f.""AvailableActions"") AS e
                    WHERE NOT (e @> '{""Key"":""margin""}'))
                WHERE ""Key"" = 'sales';");
        }
    }
}
