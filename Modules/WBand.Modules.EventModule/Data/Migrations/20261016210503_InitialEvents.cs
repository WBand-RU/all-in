using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WBand.Modules.EventModule.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "events");

            migrationBuilder.CreateTable(
                name: "performance_events",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BandId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Venue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    SongIds = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_performance_events", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_performance_events_BandId_StartsAt",
                schema: "events",
                table: "performance_events",
                columns: new[] { "BandId", "StartsAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "performance_events",
                schema: "events");
        }
    }
}
