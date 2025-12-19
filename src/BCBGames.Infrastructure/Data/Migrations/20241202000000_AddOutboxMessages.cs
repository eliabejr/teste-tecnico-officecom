using BCBGames.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BCBGames.Infrastructure.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20241202000000_AddOutboxMessages")]
public partial class AddOutboxMessages : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "OutboxMessages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                EventType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                IdempotencyKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                PayloadJson = table.Column<string>(type: "text", nullable: false),
                ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                Attempts = table.Column<int>(type: "integer", nullable: false),
                LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OutboxMessages", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_OutboxMessages_ProcessedAt",
            table: "OutboxMessages",
            column: "ProcessedAt");

        migrationBuilder.CreateIndex(
            name: "IX_OutboxMessages_IdempotencyKey_EventType_AggregateId",
            table: "OutboxMessages",
            columns: new[] { "IdempotencyKey", "EventType", "AggregateId" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "OutboxMessages");
    }
}
