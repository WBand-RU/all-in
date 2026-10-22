using Microsoft.EntityFrameworkCore;
using WBand.Modules.SongModule;
using Xunit;

namespace WBand.WebAPI.Tests;

public sealed class SongModulePersistenceTests
{
    [Fact]
    public void Model_WhenMapped_OwnsFiveEntitiesAndPreservesCompositeCellReferences()
    {
        using var db = CreateContext();
        Assert.Equal(5, db.Model.GetEntityTypes().Count());
        Assert.All(db.Model.GetEntityTypes(), entity =>
        {
            Assert.Equal(typeof(SongModule).Assembly, entity.ClrType.Assembly);
            Assert.Equal("public", entity.GetSchema());
        });
        var keys = db.Model.FindEntityType(typeof(SongContentCell))!.GetForeignKeys().ToArray();
        Assert.Equal(2, keys.Length);
        Assert.All(keys, key =>
        {
            Assert.Equal(2, key.Properties.Count);
            Assert.Equal(nameof(SongContentCell.SongId), key.Properties[0].Name);
            Assert.Equal(new[] { "SongId", "Id" }, key.PrincipalKey.Properties.Select(property => property.Name));
        });
        Assert.Contains(keys, key => key.PrincipalEntityType.ClrType == typeof(SongSection));
        Assert.Contains(keys, key => key.PrincipalEntityType.ClrType == typeof(Partition));
    }

    [Fact]
    public async Task Insert_WhenTransactionIsMissing_RejectsBeforeConnecting()
    {
        await using var db = CreateContext();
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => db.InsertAsync(new Song()));
        Assert.Contains("explicit transaction", error.Message);
    }

    private static SongDataContext CreateContext() => new(new DbContextOptionsBuilder<SongDataContext>()
        .UseNpgsql("Host=localhost;Database=unused;Username=unused").Options);
}
