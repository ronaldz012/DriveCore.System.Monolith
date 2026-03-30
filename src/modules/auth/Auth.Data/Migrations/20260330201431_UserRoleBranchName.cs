using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Data.Migrations
{
    /// <inheritdoc />
    public partial class UserRoleBranchName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Roles_RoleId",
                table: "UserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Users_UserId",
                table: "UserRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserRoles",
                table: "UserRoles");

            migrationBuilder.RenameTable(
                name: "UserRoles",
                newName: "UserBranchRoles");

            migrationBuilder.RenameIndex(
                name: "IX_UserRoles_UserId",
                table: "UserBranchRoles",
                newName: "IX_UserBranchRoles_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserBranchRoles",
                newName: "IX_UserBranchRoles_RoleId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserBranchRoles",
                table: "UserBranchRoles",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserBranchRoles_Roles_RoleId",
                table: "UserBranchRoles",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserBranchRoles_Users_UserId",
                table: "UserBranchRoles",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserBranchRoles_Roles_RoleId",
                table: "UserBranchRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_UserBranchRoles_Users_UserId",
                table: "UserBranchRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserBranchRoles",
                table: "UserBranchRoles");

            migrationBuilder.RenameTable(
                name: "UserBranchRoles",
                newName: "UserRoles");

            migrationBuilder.RenameIndex(
                name: "IX_UserBranchRoles_UserId",
                table: "UserRoles",
                newName: "IX_UserRoles_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_UserBranchRoles_RoleId",
                table: "UserRoles",
                newName: "IX_UserRoles_RoleId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserRoles",
                table: "UserRoles",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Roles_RoleId",
                table: "UserRoles",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Users_UserId",
                table: "UserRoles",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
