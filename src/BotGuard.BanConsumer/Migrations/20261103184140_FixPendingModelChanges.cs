using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BotGuard.BanConsumer.Migrations
{
    /// <inheritdoc />
    public partial class FixPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Incidents_IpAddress_DetectedAtUtc",
                table: "Incidents");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_IpAddress_DetectedAtUtc",
                table: "Incidents",
                columns: new[] { "IpAddress", "DetectedAtUtc" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Incidents_IpAddress_DetectedAtUtc",
                table: "Incidents");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_IpAddress_DetectedAtUtc",
                table: "Incidents",
                columns: new[] { "IpAddress", "DetectedAtUtc" });
        }
    }
}
