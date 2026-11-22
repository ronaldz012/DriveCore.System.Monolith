using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace System.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSalePayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SalePayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SaleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Method = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByName = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalePayments_Sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalePayments_SaleId",
                table: "SalePayments",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_SalePayments_TenantId_SaleId_Method",
                table: "SalePayments",
                columns: new[] { "TenantId", "SaleId", "Method" },
                unique: true);

            // Backfill: every existing sale (returns included, whose TotalAmount is
            // negative) gets one payment line matching its current single method.
            // Idempotent: guarded so re-running against a partially backfilled copy
            // does not duplicate lines. Satisfies the unique (TenantId, SaleId, Method)
            // index: each sale produces exactly one line.
            migrationBuilder.Sql(@"
                INSERT INTO ""SalePayments""
                    (""Id"", ""TenantId"", ""SaleId"", ""Method"", ""Amount"", ""CreatedAt"", ""CreatedBy"", ""CreatedByName"")
                SELECT gen_random_uuid(), s.""TenantId"", s.""Id"", s.""PaymentMethod"", s.""TotalAmount"",
                       s.""CreatedAt"", s.""CreatedBy"", s.""CreatedByName""
                FROM ""Sales"" AS s
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""SalePayments"" AS sp
                    WHERE sp.""SaleId"" = s.""Id"");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalePayments");
        }
    }
}
