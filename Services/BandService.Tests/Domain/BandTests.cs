using BandService.Domain;
using Shared;

namespace BandService.Tests.Domain;

[TestClass]
public class BandTests
{
    private static Band CreateBand(UserId? createdBy = null)
    {
        return new Band { Id = BandId.New(), CreatedBy = createdBy ?? UserId.New() };
    }

    [TestMethod]
    public void WhenBandIsCreatedThenIdIsAssigned()
    {
        var id = BandId.New();

        var band = new Band { Id = id, CreatedBy = UserId.New() };

        Assert.AreEqual(id, band.Id);
    }

    [TestMethod]
    public void WhenBandIsCreatedThenCreatedByIsAssigned()
    {
        var createdBy = UserId.New();

        var band = CreateBand(createdBy: createdBy);

        Assert.AreEqual(createdBy, band.CreatedBy);
    }

    [TestMethod]
    public void WhenBandIsCreatedThenCreatedAtIsSetToCurrentUtcTime()
    {
        var before = DateTime.UtcNow;

        var band = CreateBand();

        var after = DateTime.UtcNow;
        Assert.IsTrue(band.CreatedAt >= before && band.CreatedAt <= after);
        Assert.AreEqual(DateTimeKind.Utc, band.CreatedAt.Kind);
    }

    [TestMethod]
    public void WhenBandIsCreatedThenUpdateAtIsNull()
    {
        var band = CreateBand();

        Assert.IsNull(band.UpdateAt);
    }

    [TestMethod]
    public void WhenBandIsCreatedThenUpdateByIsNull()
    {
        var band = CreateBand();

        Assert.IsNull(band.UpdateBy);
    }

    [TestMethod]
    public void WhenSetNameIsCalledThenNameIsUpdated()
    {
        var band = CreateBand();
        var newName = BandName.Create("New Name");

        band.SetName(newName, UserId.New());

        Assert.AreEqual(newName.Value, band.Name.Value);
    }

    [TestMethod]
    public void WhenSetNameIsCalledThenUpdateByIsSetToProvidedUser()
    {
        var band = CreateBand();
        var userId = UserId.New();

        band.SetName(BandName.Create("Another"), userId);

        Assert.IsNotNull(band.UpdateBy);
        Assert.AreEqual(userId, band.UpdateBy!.Value);
    }

    [TestMethod]
    public void WhenSetNameIsCalledThenUpdateAtIsSetToCurrentUtcTime()
    {
        var band = CreateBand();
        var before = DateTime.UtcNow;

        band.SetName(BandName.Create("Updated"), UserId.New());

        var after = DateTime.UtcNow;
        Assert.IsNotNull(band.UpdateAt);
        Assert.IsTrue(band.UpdateAt!.Value >= before && band.UpdateAt.Value <= after);
        Assert.AreEqual(DateTimeKind.Utc, band.UpdateAt.Value.Kind);
    }

    [TestMethod]
    public void WhenSetNameIsCalledThenCreatedAtIsNotChanged()
    {
        var band = CreateBand();
        var originalCreatedAt = band.CreatedAt;

        band.SetName(BandName.Create("Updated"), UserId.New());

        Assert.AreEqual(originalCreatedAt, band.CreatedAt);
    }

    [TestMethod]
    public void WhenSetNameIsCalledThenCreatedByIsNotChanged()
    {
        var createdBy = UserId.New();
        var band = CreateBand(createdBy: createdBy);

        band.SetName(BandName.Create("Updated"), UserId.New());

        Assert.AreEqual(createdBy, band.CreatedBy);
    }

    [TestMethod]
    public void WhenSetNameIsCalledMultipleTimesThenLatestValuesAreKept()
    {
        var band = CreateBand();
        var firstUser = UserId.New();
        var secondUser = UserId.New();

        band.SetName(BandName.Create("First Update"), firstUser);
        var firstUpdateAt = band.UpdateAt;

        Thread.Sleep(10);

        band.SetName(BandName.Create("Second Update"), secondUser);

        Assert.AreEqual("Second Update", band.Name.Value);
        Assert.AreEqual(secondUser, band.UpdateBy!.Value);
        Assert.IsTrue(band.UpdateAt!.Value >= firstUpdateAt!.Value);
    }
}
