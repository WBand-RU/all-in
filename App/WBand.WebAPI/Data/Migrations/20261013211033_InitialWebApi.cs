using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WBand.WebAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialWebApi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "users",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    keycloak_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    first_name = table.Column<string>(type: "text", nullable: false),
                    last_name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.UniqueConstraint("AK_users_keycloak_id", x => x.keycloak_id);
                });

            migrationBuilder.CreateTable(
                name: "bands",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bands", x => x.id);
                    table.ForeignKey(
                        name: "FK_bands_users_created_by",
                        column: x => x.created_by,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "keycloak_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "band_departures",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    band_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    reason = table.Column<int>(type: "integer", nullable: false),
                    left_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    rejoined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_band_departures", x => x.id);
                    table.ForeignKey(
                        name: "FK_band_departures_bands_band_id",
                        column: x => x.band_id,
                        principalSchema: "public",
                        principalTable: "bands",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_band_departures_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "keycloak_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "band_invitations",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    band_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inviter_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inviter_email = table.Column<string>(type: "text", nullable: false),
                    invitee_email = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    responded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_band_invitations", x => x.id);
                    table.ForeignKey(
                        name: "FK_band_invitations_bands_band_id",
                        column: x => x.band_id,
                        principalSchema: "public",
                        principalTable: "bands",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_band_invitations_users_inviter_id",
                        column: x => x.inviter_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "keycloak_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "band_members",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    band_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<int>(type: "integer", nullable: false),
                    joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_band_members", x => x.id);
                    table.ForeignKey(
                        name: "FK_band_members_bands_band_id",
                        column: x => x.band_id,
                        principalSchema: "public",
                        principalTable: "bands",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_band_members_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "keycloak_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "songs",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    band_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    authors = table.Column<string[]>(type: "text[]", nullable: false),
                    key = table.Column<string>(type: "text", nullable: true),
                    bpm = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    content_version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_songs", x => x.id);
                    table.ForeignKey(
                        name: "FK_songs_bands_band_id",
                        column: x => x.band_id,
                        principalSchema: "public",
                        principalTable: "bands",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_songs_users_created_by",
                        column: x => x.created_by,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "keycloak_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_songs_users_deleted_by",
                        column: x => x.deleted_by,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "keycloak_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_songs_users_updated_by",
                        column: x => x.updated_by,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "keycloak_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "song_revisions",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    song_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content_version = table.Column<long>(type: "bigint", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    authors = table.Column<string[]>(type: "text[]", nullable: false),
                    key = table.Column<string>(type: "text", nullable: true),
                    bpm = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_song_revisions", x => x.id);
                    table.ForeignKey(
                        name: "FK_song_revisions_songs_song_id",
                        column: x => x.song_id,
                        principalSchema: "public",
                        principalTable: "songs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_song_revisions_users_created_by",
                        column: x => x.created_by,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "keycloak_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "song_sections",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    song_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    lyrics = table.Column<string>(type: "text", nullable: true),
                    chords = table.Column<string>(type: "text", nullable: true),
                    revision = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_band_departures_band_id_user_id",
                schema: "public",
                table: "band_departures",
                columns: new[] { "band_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "IX_band_departures_user_id",
                schema: "public",
                table: "band_departures",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_band_invitations_band_id_invitee_email",
                schema: "public",
                table: "band_invitations",
                columns: new[] { "band_id", "invitee_email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_band_invitations_inviter_id",
                schema: "public",
                table: "band_invitations",
                column: "inviter_id");

            migrationBuilder.CreateIndex(
                name: "IX_band_members_band_id",
                schema: "public",
                table: "band_members",
                column: "band_id",
                unique: true,
                filter: "role = 0");

            migrationBuilder.CreateIndex(
                name: "IX_band_members_band_id_email",
                schema: "public",
                table: "band_members",
                columns: new[] { "band_id", "email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_band_members_band_id_user_id",
                schema: "public",
                table: "band_members",
                columns: new[] { "band_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_band_members_user_id",
                schema: "public",
                table: "band_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_bands_created_by",
                schema: "public",
                table: "bands",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_song_revisions_created_by",
                schema: "public",
                table: "song_revisions",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_song_revisions_song_id_content_version",
                schema: "public",
                table: "song_revisions",
                columns: new[] { "song_id", "content_version" },
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_songs_band_id_title",
                schema: "public",
                table: "songs",
                columns: new[] { "band_id", "title" });

            migrationBuilder.CreateIndex(
                name: "IX_songs_created_by",
                schema: "public",
                table: "songs",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_songs_deleted_by",
                schema: "public",
                table: "songs",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "IX_songs_updated_by",
                schema: "public",
                table: "songs",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                schema: "public",
                table: "users",
                column: "email");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "band_departures",
                schema: "public");

            migrationBuilder.DropTable(
                name: "band_invitations",
                schema: "public");

            migrationBuilder.DropTable(
                name: "band_members",
                schema: "public");

            migrationBuilder.DropTable(
                name: "song_revisions",
                schema: "public");

            migrationBuilder.DropTable(
                name: "song_sections",
                schema: "public");

            migrationBuilder.DropTable(
                name: "songs",
                schema: "public");

            migrationBuilder.DropTable(
                name: "bands",
                schema: "public");

            migrationBuilder.DropTable(
                name: "users",
                schema: "public");
        }
    }
}
