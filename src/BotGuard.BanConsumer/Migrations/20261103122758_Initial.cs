using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BotGuard.BanConsumer.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Incidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IpAddress = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<int>(type: "integer", nullable: false),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    DetectedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    HandledAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incidents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_IpAddress_DetectedAtUtc",
                table: "Incidents",
                columns: new[] { "IpAddress", "DetectedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Incidents");
        }
    }
}
