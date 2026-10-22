using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WBand.Modules.BandModule.Data.Migrations;

/// <summary>Creates owned storage or atomically adopts all legacy band tables without copying data.</summary>
public partial class InitialBands : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        CREATE SCHEMA IF NOT EXISTS bands;
        DO $bands$
        DECLARE
            table_name text;
            legacy_count integer := 0;
            owned_count integer := 0;
        BEGIN
            FOREACH table_name IN ARRAY ARRAY['users', 'bands', 'band_members', 'band_departures', 'band_invitations'] LOOP
                IF to_regclass(format('public.%I', table_name)) IS NOT NULL THEN legacy_count := legacy_count + 1; END IF;
                IF to_regclass(format('bands.%I', table_name)) IS NOT NULL THEN owned_count := owned_count + 1; END IF;
            END LOOP;
            IF owned_count <> 0 OR legacy_count NOT IN (0, 5) THEN
                RAISE EXCEPTION 'Cannot adopt band storage: conflicting or incomplete public/bands tables';
            END IF;
            IF legacy_count = 5 THEN
                FOREACH table_name IN ARRAY ARRAY['users', 'bands', 'band_members', 'band_departures', 'band_invitations'] LOOP
                    EXECUTE format('ALTER TABLE public.%I SET SCHEMA bands', table_name);
                END LOOP;
            ELSE
                CREATE TABLE bands.users (
                    id uuid NOT NULL,
                    keycloak_id uuid NOT NULL,
                    email text NOT NULL,
                    created_at timestamp with time zone NOT NULL,
                    updated_at timestamp with time zone NOT NULL,
                    last_login_at timestamp with time zone NOT NULL,
                    first_name text NOT NULL,
                    last_name text NOT NULL,
                    CONSTRAINT "PK_users" PRIMARY KEY (id),
                    CONSTRAINT "AK_users_keycloak_id" UNIQUE (keycloak_id)
                );
                
                CREATE TABLE bands.bands (
                    id uuid NOT NULL,
                    name character varying(120) NOT NULL,
                    created_by uuid NOT NULL,
                    created_at timestamp with time zone NOT NULL,
                    updated_at timestamp with time zone,
                    CONSTRAINT "PK_bands" PRIMARY KEY (id),
                    CONSTRAINT "FK_bands_users_created_by" FOREIGN KEY (created_by) REFERENCES bands.users (keycloak_id) ON DELETE RESTRICT
                );
                
                CREATE TABLE bands.band_departures (
                    id uuid NOT NULL,
                    band_id uuid NOT NULL,
                    user_id uuid NOT NULL,
                    email text NOT NULL,
                    reason integer NOT NULL,
                    left_at timestamp with time zone NOT NULL,
                    rejoined_at timestamp with time zone,
                    CONSTRAINT "PK_band_departures" PRIMARY KEY (id),
                    CONSTRAINT "FK_band_departures_bands_band_id" FOREIGN KEY (band_id) REFERENCES bands.bands (id) ON DELETE CASCADE,
                    CONSTRAINT "FK_band_departures_users_user_id" FOREIGN KEY (user_id) REFERENCES bands.users (keycloak_id) ON DELETE RESTRICT
                );
                
                CREATE TABLE bands.band_invitations (
                    id uuid NOT NULL,
                    band_id uuid NOT NULL,
                    inviter_id uuid NOT NULL,
                    inviter_email text NOT NULL,
                    invitee_email text NOT NULL,
                    role integer NOT NULL,
                    status integer NOT NULL,
                    created_at timestamp with time zone NOT NULL,
                    expires_at timestamp with time zone NOT NULL,
                    responded_at timestamp with time zone,
                    CONSTRAINT "PK_band_invitations" PRIMARY KEY (id),
                    CONSTRAINT "FK_band_invitations_bands_band_id" FOREIGN KEY (band_id) REFERENCES bands.bands (id) ON DELETE CASCADE,
                    CONSTRAINT "FK_band_invitations_users_inviter_id" FOREIGN KEY (inviter_id) REFERENCES bands.users (keycloak_id) ON DELETE RESTRICT
                );
                
                CREATE TABLE bands.band_members (
                    id uuid NOT NULL,
                    band_id uuid NOT NULL,
                    user_id uuid NOT NULL,
                    email text NOT NULL,
                    role integer NOT NULL,
                    joined_at timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_band_members" PRIMARY KEY (id),
                    CONSTRAINT "FK_band_members_bands_band_id" FOREIGN KEY (band_id) REFERENCES bands.bands (id) ON DELETE CASCADE,
                    CONSTRAINT "FK_band_members_users_user_id" FOREIGN KEY (user_id) REFERENCES bands.users (keycloak_id) ON DELETE RESTRICT
                );
                
                CREATE INDEX "IX_band_departures_band_id_user_id" ON bands.band_departures (band_id, user_id);
                
                CREATE INDEX "IX_band_departures_user_id" ON bands.band_departures (user_id);
                
                CREATE UNIQUE INDEX "IX_band_invitations_band_id_invitee_email" ON bands.band_invitations (band_id, invitee_email);
                
                CREATE INDEX "IX_band_invitations_inviter_id" ON bands.band_invitations (inviter_id);
                
                CREATE UNIQUE INDEX "IX_band_members_band_id" ON bands.band_members (band_id) WHERE role = 0;
                
                CREATE UNIQUE INDEX "IX_band_members_band_id_email" ON bands.band_members (band_id, email);
                
                CREATE UNIQUE INDEX "IX_band_members_band_id_user_id" ON bands.band_members (band_id, user_id);
                
                CREATE INDEX "IX_band_members_user_id" ON bands.band_members (user_id);
                
                CREATE INDEX "IX_bands_created_by" ON bands.bands (created_by);
                
                CREATE INDEX "IX_users_email" ON bands.users (email);
            END IF;
        END $bands$;
        """);

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        DO $bands$
        DECLARE table_name text;
        BEGIN
            FOREACH table_name IN ARRAY ARRAY['users', 'bands', 'band_members', 'band_departures', 'band_invitations'] LOOP
                IF to_regclass(format('public.%I', table_name)) IS NOT NULL THEN
                    RAISE EXCEPTION 'Cannot return band storage: public table already exists';
                END IF;
            END LOOP;
            FOREACH table_name IN ARRAY ARRAY['users', 'bands', 'band_members', 'band_departures', 'band_invitations'] LOOP
                EXECUTE format('ALTER TABLE bands.%I SET SCHEMA public', table_name);
            END LOOP;
        END $bands$;
        """);
}
