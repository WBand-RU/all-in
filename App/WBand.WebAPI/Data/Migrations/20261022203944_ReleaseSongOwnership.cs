using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WBand.WebAPI.Data.Migrations;

/// <summary>Releases song EF ownership while retaining all tables, indexes and database constraints.</summary>
public partial class ReleaseSongOwnership : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // SongModule adopts the existing storage. Removing the host model must not drop data.
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // The retained tables remain compatible with the preceding host snapshot.
    }
}
