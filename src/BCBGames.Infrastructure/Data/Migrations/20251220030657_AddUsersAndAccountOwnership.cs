using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BCBGames.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUsersAndAccountOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var legacyUserId = new Guid("00000000-0000-0000-0000-000000000001");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Accounts",
                type: "uuid",
                nullable: false,
                defaultValue: legacyUserId);

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            // Backward-compat: existing Accounts rows need a valid FK before adding the constraint.
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Email", "PasswordHash", "Role", "CreatedAt" },
                values: new object[]
                {
                    legacyUserId,
                    "legacy@local",
                    "DISABLED",
                    "System",
                    DateTime.UtcNow
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_UserId",
                table: "Accounts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_Users_UserId",
                table: "Accounts",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_Users_UserId",
                table: "Accounts");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_UserId",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Accounts");
        }
    }
}
