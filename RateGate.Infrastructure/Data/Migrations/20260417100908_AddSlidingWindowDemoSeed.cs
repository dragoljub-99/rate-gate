using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RateGate.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSlidingWindowDemoSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "policies",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Default demo policy (token bucket)");

            migrationBuilder.InsertData(
                table: "policies",
                columns: new[] { "Id", "Algorithm", "BurstLimit", "CreatedAtUtc", "EndpointPattern", "Limit", "Name", "UserId", "WindowInSeconds" },
                values: new object[] { 2, 2, null, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "/sliding-demo", 10, "Sliding window demo policy", 1, 10 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "policies",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.UpdateData(
                table: "policies",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Default demo policy");
        }
    }
}
