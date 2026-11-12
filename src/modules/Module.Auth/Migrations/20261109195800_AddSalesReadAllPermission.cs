using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.Auth.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesReadAllPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data-only migration: introduces the field-level 'readAll' permission
            // used by SaleController.GetStats ("sales" + "readAll" allows seeing
            // statistics from every seller; without it the actor only sees their own).
            //
            // Every statement is idempotent (guarded with NOT @> / NOT x = ANY(...))
            // because this migration runs BEFORE seeders on a fresh database and may
            // be re-run against a partially migrated copy during rehearsal.

            // 1. Feature catalog: add the 'readAll' action to the 'sales' feature.
            //    AvailableActions is a jsonb column: [ { Key, Description, DisplayName } ].
            migrationBuilder.Sql(@"
                UPDATE ""Features""
                SET ""AvailableActions"" = ""AvailableActions"" ||
                    '{""Key"":""readAll"",""Description"":""Permite ver el rendimiento y las estadísticas de todos los vendedores."",""DisplayName"":""Ver Todas las Ventas""}'::jsonb
                WHERE ""Key"" = 'sales'
                  AND NOT (""AvailableActions"" @> '[{""Key"":""readAll""}]'::jsonb);");

            // 2. Existing roles. Role templates are system roles (fixed names),
            //    so a name-based bulk update is deterministic.
            //
            //    Supervisor: append 'readAll' to the existing 'sales' permission row.
            //    (Permissions is a text[] column in public.RoleFeaturePermissions.)
            //    Vendedor keeps 'read' only.
            migrationBuilder.Sql(@"
                UPDATE ""RoleFeaturePermissions"" AS rfp
                SET ""Permissions"" = array_append(""Permissions"", 'readAll')
                FROM ""Roles"" AS r
                WHERE rfp.""RoleId"" = r.""Id""
                  AND r.""Name"" = 'Supervisor'
                  AND r.""DeletedAt"" IS NULL
                  AND rfp.""FeatureKey"" = 'sales'
                  AND NOT ('readAll' = ANY(rfp.""Permissions""));");

            // 3. Plan "Basic": fix the persisted role template so tenants created
            //    from now on get the permission by default.
            //    The array layout is fixed by PlanSeeder: [0]=Vendedor, [1]=Almacenero,
            //    [2]=Supervisor; the 'sales' entry is Permissions[4]
            //    (products, transfers, receptions, pos, sales, closures).
            migrationBuilder.Sql(@"
                UPDATE ""Plans""
                SET ""DefaultRolesTemplate"" = jsonb_set(
                    ""DefaultRolesTemplate"",
                    '{2,Permissions,4,Actions}',
                    '[""read"",""readAll"",""margin""]'::jsonb)
                WHERE ""Name"" = 'Basic'
                  AND ""DefaultRolesTemplate"" @> '[{""Name"":""Supervisor""}]'::jsonb
                  AND ""DefaultRolesTemplate"" #> '{2,Permissions,4,FeatureKey}' = '""sales""'::jsonb
                  AND NOT (""DefaultRolesTemplate"" #> '{2,Permissions,4,Actions}' @> '""readAll""'::jsonb);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Best-effort reversal of the data changes made by Up. On a fresh
            // database the seeders ran after this migration, so reverting also
            // removes what the seeders added.

            // 3. Plan "Basic": restore the former Supervisor template actions.
            migrationBuilder.Sql(@"
                UPDATE ""Plans""
                SET ""DefaultRolesTemplate"" = jsonb_set(
                    ""DefaultRolesTemplate"",
                    '{2,Permissions,4,Actions}',
                    '[""read"",""margin""]'::jsonb)
                WHERE ""Name"" = 'Basic'
                  AND ""DefaultRolesTemplate"" #> '{2,Permissions,4,FeatureKey}' = '""sales""'::jsonb;");

            // 2. Existing roles: remove 'readAll' from Supervisor.
            migrationBuilder.Sql(@"
                UPDATE ""RoleFeaturePermissions"" AS rfp
                SET ""Permissions"" = array_remove(rfp.""Permissions"", 'readAll')
                FROM ""Roles"" AS r
                WHERE rfp.""RoleId"" = r.""Id""
                  AND r.""Name"" = 'Supervisor'
                  AND rfp.""FeatureKey"" = 'sales';");

            // 1. Feature catalog: drop the 'readAll' action (rebuild the array).
            migrationBuilder.Sql(@"
                UPDATE ""Features"" AS f
                SET ""AvailableActions"" = (
                    SELECT COALESCE(jsonb_agg(e), '[]'::jsonb)
                    FROM jsonb_array_elements(f.""AvailableActions"") AS e
                    WHERE NOT (e @> '{""Key"":""readAll""}'))
                WHERE ""Key"" = 'sales';");
        }
    }
}
