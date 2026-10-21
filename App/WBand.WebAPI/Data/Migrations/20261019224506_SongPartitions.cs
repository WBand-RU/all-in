using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WBand.WebAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class SongPartitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "song_partitions",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    song_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    revision = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_song_partitions", x => x.id);
                    table.ForeignKey(
                        name: "FK_song_partitions_songs_song_id",
                        column: x => x.song_id,
                        principalSchema: "public",
                        principalTable: "songs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_song_partitions_users_created_by",
                        column: x => x.created_by,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "keycloak_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_song_partitions_users_updated_by",
                        column: x => x.updated_by,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "keycloak_id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Preserve each old section and split distinct lyrics/chords into adjacent parts.
            // Keep whitespace, attribution and revisions; even empty sections survive the upgrade.
            migrationBuilder.Sql("""
                insert into public.song_partitions
                    (id, song_id, name, "order", content, revision, created_at, created_by, updated_at, updated_by)
                select case when part.kind = 0 then section.id else gen_random_uuid() end,
                    section.song_id, part.name,
                    (row_number() over (partition by section.song_id
                        order by section."order", section.created_at, section.id, part.kind) - 1)::integer,
                    part.content, section.revision, section.created_at, section.created_by,
                    section.updated_at, section.updated_by
                from public.song_sections section
                cross join lateral (
                    select 0 as kind,
                        case when coalesce(section.lyrics, '') <> '' then left('Слова — ' || section.name, 100)
                            when coalesce(section.chords, '') <> '' then left('Аккорды — ' || section.name, 100)
                            else section.name end as name,
                        case when coalesce(section.lyrics, '') <> '' then section.lyrics
                            else coalesce(section.chords, '') end as content
                    union all
                    select 1, left('Аккорды — ' || section.name, 100), section.chords
                    where coalesce(section.lyrics, '') <> '' and coalesce(section.chords, '') <> ''
                        and section.lyrics <> section.chords
                ) part;
                """);

            // Retain exact section boundaries for the following matrix migration on fresh upgrades.
            migrationBuilder.Sql("""
                create table public.song_matrix_legacy_sections as select * from public.song_sections;
                create table public.song_matrix_legacy_partitions as select id from public.song_partitions;
                """);
            migrationBuilder.DropTable(name: "song_sections", schema: "public");

            migrationBuilder.CreateIndex(
                name: "IX_song_partitions_created_by",
                schema: "public",
                table: "song_partitions",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_song_partitions_song_id_order",
                schema: "public",
                table: "song_partitions",
                columns: new[] { "song_id", "order" });

            migrationBuilder.CreateIndex(
                name: "IX_song_partitions_updated_by",
                schema: "public",
                table: "song_partitions",
                column: "updated_by");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "song_sections",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    chords = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    lyrics = table.Column<string>(type: "text", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    revision = table.Column<long>(type: "bigint", nullable: false),
                    song_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_song_sections", x => x.id);
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

            migrationBuilder.Sql("drop table if exists public.song_matrix_legacy_sections, public.song_matrix_legacy_partitions");
            // Older clients can still read all partition content after a downgrade.
            migrationBuilder.Sql("""
                insert into public.song_sections
                    (id, song_id, name, "order", lyrics, chords, revision, created_at, created_by, updated_at, updated_by)
                select id, song_id, name, "order", content, null, revision, created_at, created_by, updated_at, updated_by
                from public.song_partitions;
                """);
            migrationBuilder.DropTable(name: "song_partitions", schema: "public");

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
        }
    }
}
