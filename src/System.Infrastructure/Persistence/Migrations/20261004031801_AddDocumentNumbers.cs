using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace System.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentNumbers : Migration
    {
        /// <summary>
        /// Numbered tables and their counter key. Header rows only, never their items.
        /// Alphabetical because the tables have no dependencies between them.
        /// </summary>
        private static readonly (string Table, string CounterKey)[] NumberedTables =
        {
            ("CashRegisterClosures", "CashClose"),
            ("Sales", "Sale"),
            ("StockReceptions", "Reception"),
            ("StockTransfers", "Transfer"),
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Counters. Created FIRST because step 4 of each table inserts into it.
            //    No schema qualifier: the name comes from the connection's Search Path,
            //    same as the previous migrations. No seeded rows: the generators create
            //    them on demand through ON CONFLICT.
            migrationBuilder.CreateTable(
                name: "DocumentCounters",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CounterKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LastNumber = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentCounters", x => new { x.TenantId, x.CounterKey });
                });

            foreach (var (table, counterKey) in NumberedTables)
            {
                // 2. Column NULLABLE first. NOT defaultValue 0: every existing row would
                //    end up at 0 and the unique index in step 6 would fail.
                migrationBuilder.AddColumn<int>(
                    name: "Number",
                    table: table,
                    type: "integer",
                    nullable: true);

                // 3. Backfill. ROW_NUMBER per tenant in real insertion order. "CreatedAt" is
                //    the audit column of the INSERT on all four tables, and "Id" breaks ties
                //    so the order is deterministic. Raw SQL ignores EF's global query
                //    filter, so soft-deleted rows are included, which is what we want: a
                //    number is never reused.
                migrationBuilder.Sql($"""
                    WITH n AS (
                        SELECT "Id",
                               ROW_NUMBER() OVER (PARTITION BY "TenantId"
                                                  ORDER BY "CreatedAt", "Id") AS rn
                        FROM "{table}"
                    )
                    UPDATE "{table}" t
                    SET "Number" = n.rn::int
                    FROM n
                    WHERE t."Id" = n."Id";
                    """);

                // 4. Align the counter with the highest number already assigned. Only for
                //    tenants that already have rows of this type: a tenant with none gets
                //    no row here, and the generator's ON CONFLICT gives it 1 on the first
                //    record.
                migrationBuilder.Sql($"""
                    INSERT INTO "DocumentCounters" ("TenantId", "CounterKey", "LastNumber")
                    SELECT "TenantId", '{counterKey}', MAX("Number")
                    FROM "{table}"
                    GROUP BY "TenantId";
                    """);

                // 5. No nulls left.
                migrationBuilder.AlterColumn<int>(
                    name: "Number",
                    table: table,
                    type: "integer",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "integer",
                    oldNullable: true);

                // 6. Safety net against duplicates. Deliberately unfiltered: a partial
                //    index would let a soft-deleted row's number be handed out again.
                //    The name is the one EF derives by convention.
                migrationBuilder.CreateIndex(
                    name: $"IX_{table}_TenantId_Number",
                    table: table,
                    columns: new[] { "TenantId", "Number" },
                    unique: true);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var (table, _) in NumberedTables)
            {
                migrationBuilder.DropIndex(
                    name: $"IX_{table}_TenantId_Number",
                    table: table);

                migrationBuilder.DropColumn(
                    name: "Number",
                    table: table);
            }

            migrationBuilder.DropTable(
                name: "DocumentCounters");
        }
    }
}