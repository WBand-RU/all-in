using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WBand.Modules.SongModule.Data.Migrations;

/// <summary>Creates owned song storage or adopts all legacy tables atomically without copying data.</summary>
public partial class InitialSongs : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        CREATE SCHEMA IF NOT EXISTS songs;
        DO $songs$
        DECLARE
            table_name text;
            legacy_count integer := 0;
            owned_count integer := 0;
        BEGIN
            FOREACH table_name IN ARRAY ARRAY['songs', 'song_sections', 'song_partitions', 'song_content_cells', 'song_revisions'] LOOP
                IF to_regclass(format('public.%I', table_name)) IS NOT NULL THEN legacy_count := legacy_count + 1; END IF;
                IF to_regclass(format('songs.%I', table_name)) IS NOT NULL THEN owned_count := owned_count + 1; END IF;
            END LOOP;
            IF owned_count <> 0 OR legacy_count NOT IN (0, 5) THEN
                RAISE EXCEPTION 'Cannot adopt song storage: conflicting or incomplete public/songs tables';
            END IF;
            IF to_regclass('bands.users') IS NULL OR to_regclass('bands.bands') IS NULL THEN
                RAISE EXCEPTION 'Apply band migrations before song storage migration';
            END IF;
            IF legacy_count = 5 THEN
                FOREACH table_name IN ARRAY ARRAY['songs', 'song_sections', 'song_partitions', 'song_content_cells', 'song_revisions'] LOOP
                    EXECUTE format('ALTER TABLE public.%I SET SCHEMA songs', table_name);
                END LOOP;
            ELSE
                -- External FKs are managed explicitly; the EF model contains only song entities.
                CREATE TABLE songs.songs (
                    id uuid NOT NULL,
                    band_id uuid NOT NULL,
                    title text NOT NULL,
                    authors text[] NOT NULL,
                    key text,
                    bpm integer,
                    status integer NOT NULL,
                    content_version bigint NOT NULL,
                    created_at timestamp with time zone NOT NULL,
                    created_by uuid NOT NULL,
                    updated_at timestamp with time zone,
                    updated_by uuid,
                    deleted_at timestamp with time zone,
                    deleted_by uuid,
                    CONSTRAINT "PK_songs" PRIMARY KEY (id)
                );

                CREATE TABLE songs.song_partitions (
                    id uuid NOT NULL,
                    song_id uuid NOT NULL,
                    name text NOT NULL,
                    "order" integer NOT NULL,
                    revision bigint NOT NULL,
                    created_at timestamp with time zone NOT NULL,
                    created_by uuid NOT NULL,
                    updated_at timestamp with time zone,
                    updated_by uuid,
                    CONSTRAINT "PK_song_partitions" PRIMARY KEY (id),
                    CONSTRAINT "AK_song_partitions_song_id_id" UNIQUE (song_id, id),
                    CONSTRAINT "FK_song_partitions_songs_song_id" FOREIGN KEY (song_id) REFERENCES songs.songs (id) ON DELETE CASCADE
                );

                CREATE TABLE songs.song_revisions (
                    id uuid NOT NULL,
                    song_id uuid NOT NULL,
                    content_version bigint NOT NULL,
                    title text NOT NULL,
                    authors text[] NOT NULL,
                    key text,
                    bpm integer,
                    status integer NOT NULL,
                    created_at timestamp with time zone NOT NULL,
                    created_by uuid NOT NULL,
                    CONSTRAINT "PK_song_revisions" PRIMARY KEY (id),
                    CONSTRAINT "FK_song_revisions_songs_song_id" FOREIGN KEY (song_id) REFERENCES songs.songs (id) ON DELETE CASCADE
                );

                CREATE TABLE songs.song_sections (
                    id uuid NOT NULL,
                    song_id uuid NOT NULL,
                    name text NOT NULL,
                    "order" integer NOT NULL,
                    revision bigint NOT NULL,
                    created_at timestamp with time zone NOT NULL,
                    created_by uuid NOT NULL,
                    updated_at timestamp with time zone,
                    updated_by uuid,
                    CONSTRAINT "PK_song_sections" PRIMARY KEY (id),
                    CONSTRAINT "AK_song_sections_song_id_id" UNIQUE (song_id, id),
                    CONSTRAINT "FK_song_sections_songs_song_id" FOREIGN KEY (song_id) REFERENCES songs.songs (id) ON DELETE CASCADE
                );

                CREATE TABLE songs.song_content_cells (
                    id uuid NOT NULL,
                    song_id uuid NOT NULL,
                    section_id uuid NOT NULL,
                    partition_id uuid NOT NULL,
                    content text NOT NULL,
                    revision bigint NOT NULL,
                    created_at timestamp with time zone NOT NULL,
                    created_by uuid NOT NULL,
                    updated_at timestamp with time zone,
                    updated_by uuid,
                    CONSTRAINT "PK_song_content_cells" PRIMARY KEY (id),
                    CONSTRAINT "FK_song_content_cells_song_partitions_song_id_partition_id" FOREIGN KEY (song_id, partition_id) REFERENCES songs.song_partitions (song_id, id) ON DELETE CASCADE,
                    CONSTRAINT "FK_song_content_cells_song_sections_song_id_section_id" FOREIGN KEY (song_id, section_id) REFERENCES songs.song_sections (song_id, id) ON DELETE CASCADE
                );

                CREATE UNIQUE INDEX "IX_song_content_cells_section_id_partition_id" ON songs.song_content_cells (section_id, partition_id);

                CREATE INDEX "IX_song_content_cells_song_id_partition_id" ON songs.song_content_cells (song_id, partition_id);

                CREATE INDEX "IX_song_content_cells_song_id_section_id" ON songs.song_content_cells (song_id, section_id);

                CREATE INDEX "IX_song_partitions_song_id_order" ON songs.song_partitions (song_id, "order");

                CREATE UNIQUE INDEX "IX_song_revisions_song_id_content_version" ON songs.song_revisions (song_id, content_version);

                CREATE INDEX "IX_song_sections_song_id_order" ON songs.song_sections (song_id, "order");

                CREATE INDEX "IX_songs_band_id_title" ON songs.songs (band_id, title);

                ALTER TABLE songs.songs ADD CONSTRAINT "FK_songs_bands_band_id" FOREIGN KEY (band_id) REFERENCES bands.bands (id) ON DELETE CASCADE;
                ALTER TABLE songs.songs ADD CONSTRAINT "FK_songs_users_created_by" FOREIGN KEY (created_by) REFERENCES bands.users (keycloak_id) ON DELETE RESTRICT;
                CREATE INDEX "IX_songs_created_by" ON songs.songs (created_by);
                ALTER TABLE songs.songs ADD CONSTRAINT "FK_songs_users_updated_by" FOREIGN KEY (updated_by) REFERENCES bands.users (keycloak_id) ON DELETE RESTRICT;
                CREATE INDEX "IX_songs_updated_by" ON songs.songs (updated_by);
                ALTER TABLE songs.songs ADD CONSTRAINT "FK_songs_users_deleted_by" FOREIGN KEY (deleted_by) REFERENCES bands.users (keycloak_id) ON DELETE RESTRICT;
                CREATE INDEX "IX_songs_deleted_by" ON songs.songs (deleted_by);
                ALTER TABLE songs.song_sections ADD CONSTRAINT "FK_song_sections_users_created_by" FOREIGN KEY (created_by) REFERENCES bands.users (keycloak_id) ON DELETE RESTRICT;
                CREATE INDEX "IX_song_sections_created_by" ON songs.song_sections (created_by);
                ALTER TABLE songs.song_sections ADD CONSTRAINT "FK_song_sections_users_updated_by" FOREIGN KEY (updated_by) REFERENCES bands.users (keycloak_id) ON DELETE RESTRICT;
                CREATE INDEX "IX_song_sections_updated_by" ON songs.song_sections (updated_by);
                ALTER TABLE songs.song_partitions ADD CONSTRAINT "FK_song_partitions_users_created_by" FOREIGN KEY (created_by) REFERENCES bands.users (keycloak_id) ON DELETE RESTRICT;
                CREATE INDEX "IX_song_partitions_created_by" ON songs.song_partitions (created_by);
                ALTER TABLE songs.song_partitions ADD CONSTRAINT "FK_song_partitions_users_updated_by" FOREIGN KEY (updated_by) REFERENCES bands.users (keycloak_id) ON DELETE RESTRICT;
                CREATE INDEX "IX_song_partitions_updated_by" ON songs.song_partitions (updated_by);
                ALTER TABLE songs.song_content_cells ADD CONSTRAINT "FK_song_content_cells_users_created_by" FOREIGN KEY (created_by) REFERENCES bands.users (keycloak_id) ON DELETE RESTRICT;
                CREATE INDEX "IX_song_content_cells_created_by" ON songs.song_content_cells (created_by);
                ALTER TABLE songs.song_content_cells ADD CONSTRAINT "FK_song_content_cells_users_updated_by" FOREIGN KEY (updated_by) REFERENCES bands.users (keycloak_id) ON DELETE RESTRICT;
                CREATE INDEX "IX_song_content_cells_updated_by" ON songs.song_content_cells (updated_by);
                ALTER TABLE songs.song_revisions ADD CONSTRAINT "FK_song_revisions_users_created_by" FOREIGN KEY (created_by) REFERENCES bands.users (keycloak_id) ON DELETE RESTRICT;
                CREATE INDEX "IX_song_revisions_created_by" ON songs.song_revisions (created_by);
            END IF;
        END $songs$;
        """);

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        DO $songs$
        DECLARE table_name text;
        BEGIN
            FOREACH table_name IN ARRAY ARRAY['songs', 'song_sections', 'song_partitions', 'song_content_cells', 'song_revisions'] LOOP
                IF to_regclass(format('public.%I', table_name)) IS NOT NULL THEN
                    RAISE EXCEPTION 'Cannot return song storage: public table already exists';
                END IF;
            END LOOP;
            FOREACH table_name IN ARRAY ARRAY['songs', 'song_sections', 'song_partitions', 'song_content_cells', 'song_revisions'] LOOP
                EXECUTE format('ALTER TABLE songs.%I SET SCHEMA public', table_name);
            END LOOP;
        END $songs$;
        """);
}
