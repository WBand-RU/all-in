using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WBand.WebAPI.Data.Migrations;

/// <summary>Releases the band EF model while retaining its tables, indexes and database foreign keys.</summary>
public partial class ReleaseBandOwnership : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // BandModule adopts these existing tables. Model removal must never scaffold DROP here.
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // The existing tables are already compatible with the previous host model.
    }
}
