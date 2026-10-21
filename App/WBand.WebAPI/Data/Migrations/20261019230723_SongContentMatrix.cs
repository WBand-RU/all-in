using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WBand.WebAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class SongContentMatrix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_song_partitions_song_id_id",
                schema: "public",
                table: "song_partitions",
                columns: new[] { "song_id", "id" });

            migrationBuilder.CreateTable(
                name: "song_sections",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    song_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    revision = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_song_sections", x => x.id);
                    table.UniqueConstraint("AK_song_sections_song_id_id", x => new { x.song_id, x.id });
                    table.ForeignKey(
                        name: "FK_song_sections_songs_song_id",
                        column: x => x.song_id,
                        principalSchema: "public",
                        principalTable: "songs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_song_sections_users_created_by",
                        column: x => x.created_by,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "keycloak_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_song_sections_users_updated_by",
                        column: x => x.updated_by,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "keycloak_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "song_content_cells",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    song_id = table.Column<Guid>(type: "uuid", nullable: false),
                    section_id = table.Column<Guid>(type: "uuid", nullable: false),
                    partition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    revision = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_song_content_cells", x => x.id);
                    table.ForeignKey(
                        name: "FK_song_content_cells_song_partitions_song_id_partition_id",
                        columns: x => new { x.song_id, x.partition_id },
                        principalSchema: "public",
                        principalTable: "song_partitions",
                        principalColumns: new[] { "song_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_song_content_cells_song_sections_song_id_section_id",
                        columns: x => new { x.song_id, x.section_id },
                        principalSchema: "public",
                        principalTable: "song_sections",
                        principalColumns: new[] { "song_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_song_content_cells_users_created_by",
                        column: x => x.created_by,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "keycloak_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_song_content_cells_users_updated_by",
                        column: x => x.updated_by,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "keycloak_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_song_content_cells_created_by",
                schema: "public",
                table: "song_content_cells",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_song_content_cells_section_id_partition_id",
                schema: "public",
                table: "song_content_cells",
                columns: new[] { "section_id", "partition_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_song_content_cells_song_id_partition_id",
                schema: "public",
                table: "song_content_cells",
                columns: new[] { "song_id", "partition_id" });

            migrationBuilder.CreateIndex(
                name: "IX_song_content_cells_song_id_section_id",
                schema: "public",
                table: "song_content_cells",
                columns: new[] { "song_id", "section_id" });

            migrationBuilder.CreateIndex(
                name: "IX_song_content_cells_updated_by",
                schema: "public",
                table: "song_content_cells",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_song_sections_created_by",
                schema: "public",
                table: "song_sections",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_song_sections_song_id_order",
                schema: "public",
                table: "song_sections",
                columns: new[] { "song_id", "order" });

            migrationBuilder.CreateIndex(
                name: "IX_song_sections_updated_by",
                schema: "public",
                table: "song_sections",
                column: "updated_by");
            MigrateContent(migrationBuilder);
            migrationBuilder.DropColumn(name: "content", schema: "public", table: "song_partitions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "content",
                schema: "public",
                table: "song_partitions",
                type: "text",
                nullable: false,
                defaultValue: "");
            migrationBuilder.Sql("""
                update public.song_partitions partition set content = coalesce((
                    select string_agg(section.name || E':\n' || cell.content, E'\n\n'
                        order by section."order", section.created_at, section.id)
                    from public.song_content_cells cell
                    join public.song_sections section on section.id = cell.section_id
                    where cell.partition_id = partition.id
                ), '');
                """);

            migrationBuilder.DropTable(
                name: "song_content_cells",
                schema: "public");

            migrationBuilder.DropTable(
                name: "song_sections",
                schema: "public");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_song_partitions_song_id_id",
                schema: "public",
                table: "song_partitions");


        }
    }
}
