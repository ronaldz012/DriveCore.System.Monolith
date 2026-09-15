using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.Auth.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchCompleteSince : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompleteSince",
                table: "Branches",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompleteSince",
                table: "Branches");
        }
    }
}
