using System.Net;
using System.Net.Http.Json;
using WBand.WebAPI.Data;
using WBand.WebAPI.Endpoints.Bands;
using WBand.WebAPI.Endpoints.Songs;
using WBand.WebAPI.Shared;
using Xunit;

namespace WBand.WebAPI.Tests;

[Collection("Web API")]
public sealed class SongMatrixApiTests(BandsApiFixture fixture)
{
    [Fact]
    public async Task Matrix_WhenAxesChange_SharesSectionsAcrossAllPartitionsAndKeepsIndependentCells()
    {
        using var owner = fixture.CreateClient(Guid.NewGuid());
        var song = await CreateSong(owner);
        var lyrics = await Create<PartitionResponse>(owner, $"/song-api/songs/{song.Id}/partitions", new CreatePartitionRequest("Слова", 0));
        var drums = await Create<PartitionResponse>(owner, $"/song-api/songs/{song.Id}/partitions", new CreatePartitionRequest("Заметки барабанщика", 1));
        var verse = await Create<SongSectionResponse>(owner, $"/song-api/songs/{song.Id}/sections", new CreateSongSectionRequest("Куплет", 0));
        var chorus = await Create<SongSectionResponse>(owner, $"/song-api/songs/{song.Id}/sections", new CreateSongSectionRequest("Припев", 1));
        const string text = "  [Am]Строка\n  вторая [G]  ";
        var verseLyrics = await SaveCell(owner, song.Id, verse.Id, lyrics.Id, text, 0);
        await SaveCell(owner, song.Id, verse.Id, drums.Id, "Сбивка", 0);
        await SaveCell(owner, song.Id, chorus.Id, lyrics.Id, "Припев", 0);
        var matrix = (await owner.GetFromJsonAsync<SongContentResponse>($"/song-api/songs/{song.Id}/content"))!;
        Assert.Equal([verse.Id, chorus.Id], matrix.Sections.Select(x => x.Id));
        Assert.Equal([lyrics.Id, drums.Id], matrix.Partitions.Select(x => x.Id));
        Assert.Equal(3, matrix.Cells.Count);
        Assert.DoesNotContain(matrix.Cells, x => x.SectionId == chorus.Id && x.PartitionId == drums.Id);
        Assert.Equal(text, Assert.Single(matrix.Cells, x => x.SectionId == verse.Id && x.PartitionId == lyrics.Id).Content);
        Assert.Equal(1, verseLyrics.Revision);

        (await owner.PutAsJsonAsync($"/song-api/sections/{verse.Id}", new UpdateSongSectionRequest("Куплет 1", 5, verse.Revision))).EnsureSuccessStatusCode();
        matrix = (await owner.GetFromJsonAsync<SongContentResponse>($"/song-api/songs/{song.Id}/content"))!;
        Assert.Equal([chorus.Id, verse.Id], matrix.Sections.Select(x => x.Id));
        Assert.Equal("Куплет 1", matrix.Sections[1].Name);
        Assert.Equal(2, matrix.Cells.Count(x => x.SectionId == verse.Id));
        (await owner.DeleteAsync($"/song-api/sections/{verse.Id}")).EnsureSuccessStatusCode();
        matrix = (await owner.GetFromJsonAsync<SongContentResponse>($"/song-api/songs/{song.Id}/content"))!;
        Assert.Equal(chorus.Id, Assert.Single(matrix.Sections).Id);
        Assert.Equal(2, matrix.Partitions.Count);
        Assert.Equal(chorus.Id, Assert.Single(matrix.Cells).SectionId);
        (await owner.DeleteAsync($"/song-api/partitions/{lyrics.Id}")).EnsureSuccessStatusCode();
        matrix = (await owner.GetFromJsonAsync<SongContentResponse>($"/song-api/songs/{song.Id}/content"))!;
        Assert.Single(matrix.Sections);
        Assert.Equal(drums.Id, Assert.Single(matrix.Partitions).Id);
        Assert.Empty(matrix.Cells);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveCell_WhenTwoEditorsShareRevision_OnlyOneSucceeds(bool existingCell)
    {
        using var owner = fixture.CreateClient(Guid.NewGuid());
        var song = await CreateSong(owner);
        var part = await Create<PartitionResponse>(owner, $"/song-api/songs/{song.Id}/partitions", new CreatePartitionRequest("Аккорды", 0));
        var section = await Create<SongSectionResponse>(owner, $"/song-api/songs/{song.Id}/sections", new CreateSongSectionRequest("Куплет", 0));
        if (existingCell) await SaveCell(owner, song.Id, section.Id, part.Id, "[Am]", 0);
        var url = CellUrl(song.Id, section.Id, part.Id);
        var results = await Task.WhenAll(owner.PutAsJsonAsync(url, new UpdateSongContentCellRequest("[C]", existingCell ? 1 : 0)),
            owner.PutAsJsonAsync(url, new UpdateSongContentCellRequest("[G]", existingCell ? 1 : 0)));
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.Conflict);
        var matrix = (await owner.GetFromJsonAsync<SongContentResponse>($"/song-api/songs/{song.Id}/content"))!;
        Assert.Equal(existingCell ? 2 : 1, Assert.Single(matrix.Cells).Revision);
    }

    [Fact]
    public async Task Matrix_WhenAccessOrReferencesAreInvalid_RejectsReadsAndWrites()
    {
        using var owner = fixture.CreateClient(Guid.NewGuid());
        using var outsider = fixture.CreateClient(Guid.NewGuid());
        using var anonymous = fixture.CreateClient();
        var song = await CreateSong(owner);
        var other = await CreateSong(owner);
        var section = await Create<SongSectionResponse>(owner, $"/song-api/songs/{song.Id}/sections", new CreateSongSectionRequest("Куплет", 0));
        var part = await Create<PartitionResponse>(owner, $"/song-api/songs/{other.Id}/partitions", new CreatePartitionRequest("Слова", 0));
        var request = new UpdateSongContentCellRequest("text", 0);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.PutAsJsonAsync(CellUrl(song.Id, section.Id, part.Id), request)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.GetAsync($"/song-api/songs/{song.Id}/content")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.PostAsJsonAsync($"/song-api/songs/{song.Id}/sections", new CreateSongSectionRequest("Denied", 0))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.PutAsJsonAsync(CellUrl(song.Id, section.Id, part.Id), request)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"/song-api/songs/{song.Id}/content")).StatusCode);
        (await owner.DeleteAsync($"/song-api/songs/{song.Id}")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/song-api/songs/{song.Id}/content")).StatusCode);
    }

    [Fact]
    public async Task SaveCell_WhenTextOrRevisionIsInvalid_RejectsRequest()
    {
        using var owner = fixture.CreateClient(Guid.NewGuid());
        var song = await CreateSong(owner);
        var section = await Create<SongSectionResponse>(owner, $"/song-api/songs/{song.Id}/sections", new CreateSongSectionRequest("Куплет", 0));
        var part = await Create<PartitionResponse>(owner, $"/song-api/songs/{song.Id}/partitions", new CreatePartitionRequest("Слова", 0));
        foreach (var request in new[] { new UpdateSongContentCellRequest(new string('a', 50_001), 0),
            new UpdateSongContentCellRequest(null!, 0), new UpdateSongContentCellRequest("text", -1) })
            Assert.Equal(HttpStatusCode.BadRequest, (await owner.PutAsJsonAsync(CellUrl(song.Id, section.Id, part.Id), request)).StatusCode);
    }

    [Fact]
    public async Task SavePartition_WhenSeveralSectionsChange_SavesThemTogetherAndLeavesOtherPartitionsAlone()
    {
        using var owner = fixture.CreateClient(Guid.NewGuid());
        var song = await CreateSong(owner);
        var part = await Create<PartitionResponse>(owner, $"/song-api/songs/{song.Id}/partitions", new CreatePartitionRequest("Слова", 0));
        var other = await Create<PartitionResponse>(owner, $"/song-api/songs/{song.Id}/partitions", new CreatePartitionRequest("Аккорды", 1));
        var verse = await Create<SongSectionResponse>(owner, $"/song-api/songs/{song.Id}/sections", new CreateSongSectionRequest("Куплет", 0));
        var chorus = await Create<SongSectionResponse>(owner, $"/song-api/songs/{song.Id}/sections", new CreateSongSectionRequest("Припев", 1));
        await SaveCell(owner, song.Id, verse.Id, other.Id, "[Am]", 0);
        var response = await owner.PutAsJsonAsync(PartitionUrl(song.Id, part.Id), new UpdatePartitionContentRequest([
            new(verse.Id, "  [Am]Куплет\n", 0), new(chorus.Id, "Припев  ", 0)]));
        response.EnsureSuccessStatusCode();
        var saved = (await response.Content.ReadFromJsonAsync<SongContentCellResponse[]>())!;
        Assert.Equal(2, saved.Length);
        Assert.All(saved, x => Assert.Equal(1, x.Revision));
        var matrix = (await owner.GetFromJsonAsync<SongContentResponse>($"/song-api/songs/{song.Id}/content"))!;
        Assert.Equal("  [Am]Куплет\n", Assert.Single(matrix.Cells, x => x.PartitionId == part.Id && x.SectionId == verse.Id).Content);
        Assert.Equal("[Am]", Assert.Single(matrix.Cells, x => x.PartitionId == other.Id).Content);
        await SaveCell(owner, song.Id, chorus.Id, part.Id, "Отдельное сохранение", 1);
        response = await owner.PutAsJsonAsync(PartitionUrl(song.Id, part.Id), new UpdatePartitionContentRequest([new(verse.Id, "Новый куплет", 1)]));
        response.EnsureSuccessStatusCode();
        matrix = (await owner.GetFromJsonAsync<SongContentResponse>($"/song-api/songs/{song.Id}/content"))!;
        Assert.Equal("Отдельное сохранение", Assert.Single(matrix.Cells, x => x.PartitionId == part.Id && x.SectionId == chorus.Id).Content);
    }

    [Fact]
    public async Task SavePartition_WhenOneSectionIsStale_RejectsEveryWriteAndReportsConflictingSections()
    {
        using var owner = fixture.CreateClient(Guid.NewGuid());
        var song = await CreateSong(owner);
        var part = await Create<PartitionResponse>(owner, $"/song-api/songs/{song.Id}/partitions", new CreatePartitionRequest("Слова", 0));
        var verse = await Create<SongSectionResponse>(owner, $"/song-api/songs/{song.Id}/sections", new CreateSongSectionRequest("Куплет", 0));
        var chorus = await Create<SongSectionResponse>(owner, $"/song-api/songs/{song.Id}/sections", new CreateSongSectionRequest("Припев", 1));
        await SaveCell(owner, song.Id, chorus.Id, part.Id, "Изменено другим участником", 0);
        var response = await owner.PutAsJsonAsync(PartitionUrl(song.Id, part.Id), new UpdatePartitionContentRequest([
            new(verse.Id, "Не должно сохраниться", 0), new(chorus.Id, "Устаревшая версия", 0)]));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var conflict = (await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("sectionIds");
        Assert.Equal(chorus.Id, Assert.Single(conflict.EnumerateArray()).GetGuid());
        var matrix = (await owner.GetFromJsonAsync<SongContentResponse>($"/song-api/songs/{song.Id}/content"))!;
        Assert.Equal("Изменено другим участником", Assert.Single(matrix.Cells).Content);
        Assert.Equal(chorus.Id, matrix.Cells[0].SectionId);
    }

    [Fact]
    public async Task SavePartition_WhenEditorsShareRevisions_OnlyOneCompleteUpdateSucceeds()
    {
        using var owner = fixture.CreateClient(Guid.NewGuid());
        var song = await CreateSong(owner);
        var part = await Create<PartitionResponse>(owner, $"/song-api/songs/{song.Id}/partitions", new CreatePartitionRequest("Слова", 0));
        var verse = await Create<SongSectionResponse>(owner, $"/song-api/songs/{song.Id}/sections", new CreateSongSectionRequest("Куплет", 0));
        var chorus = await Create<SongSectionResponse>(owner, $"/song-api/songs/{song.Id}/sections", new CreateSongSectionRequest("Припев", 1));
        var results = await Task.WhenAll(owner.PutAsJsonAsync(PartitionUrl(song.Id, part.Id), new UpdatePartitionContentRequest([
            new(verse.Id, "Первый", 0), new(chorus.Id, "Первый", 0)])),
            owner.PutAsJsonAsync(PartitionUrl(song.Id, part.Id), new UpdatePartitionContentRequest([
            new(verse.Id, "Второй", 0), new(chorus.Id, "Второй", 0)])));
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.Conflict);
        var matrix = (await owner.GetFromJsonAsync<SongContentResponse>($"/song-api/songs/{song.Id}/content"))!;
        Assert.Equal(2, matrix.Cells.Count);
        Assert.Single(matrix.Cells.Select(x => x.Content).Distinct());
    }

    [Fact]
    public async Task SavePartition_WhenAccessReferencesOrPayloadAreInvalid_RejectsTheRequest()
    {
        using var owner = fixture.CreateClient(Guid.NewGuid());
        using var outsider = fixture.CreateClient(Guid.NewGuid());
        var song = await CreateSong(owner);
        var other = await CreateSong(owner);
        var part = await Create<PartitionResponse>(owner, $"/song-api/songs/{song.Id}/partitions", new CreatePartitionRequest("Слова", 0));
        var section = await Create<SongSectionResponse>(owner, $"/song-api/songs/{song.Id}/sections", new CreateSongSectionRequest("Куплет", 0));
        var foreignSection = await Create<SongSectionResponse>(owner, $"/song-api/songs/{other.Id}/sections", new CreateSongSectionRequest("Чужая", 0));
        var url = PartitionUrl(song.Id, part.Id);
        foreach (var request in new[] {
            new UpdatePartitionContentRequest([]), new UpdatePartitionContentRequest(null!),
            new UpdatePartitionContentRequest([new(section.Id, "a", 0), new(section.Id, "b", 0)]),
            new UpdatePartitionContentRequest([null!]), new UpdatePartitionContentRequest([new(section.Id, new string('a', 50_001), 0)]),
            new UpdatePartitionContentRequest([new(section.Id, null!, 0)]), new UpdatePartitionContentRequest([new(section.Id, "a", -1)]) })
            Assert.Equal(HttpStatusCode.BadRequest, (await owner.PutAsJsonAsync(url, request)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.PutAsJsonAsync(url, new UpdatePartitionContentRequest([new(foreignSection.Id, "a", 0)]))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.PutAsJsonAsync(url, new UpdatePartitionContentRequest([new(section.Id, "a", 0)]))).StatusCode);
    }

    private static string PartitionUrl(Guid songId, Guid partitionId) => $"/song-api/songs/{songId}/partitions/{partitionId}/content";

    private static string CellUrl(Guid song, Guid section, Guid part) => $"/song-api/songs/{song}/sections/{section}/partitions/{part}";
    private static async Task<SongContentCellResponse> SaveCell(HttpClient client, Guid song, Guid section, Guid part, string content, long revision)
    {
        var result = await client.PutAsJsonAsync(CellUrl(song, section, part), new UpdateSongContentCellRequest(content, revision));
        result.EnsureSuccessStatusCode();
        return (await result.Content.ReadFromJsonAsync<SongContentCellResponse>())!;
    }
    private static async Task<T> Create<T>(HttpClient client, string url, object request)
    {
        var result = await client.PostAsJsonAsync(url, request);
        result.EnsureSuccessStatusCode();
        return (await result.Content.ReadFromJsonAsync<T>())!;
    }
    private static async Task<SongResponse> CreateSong(HttpClient client)
    {
        var band = (await Create<ApiResponse<Band>>(client, "/band-api/bands", new CreateBandRequest("Matrix " + Guid.NewGuid()))).Value;
        return (await Create<ApiResponse<SongResponse>>(client, "/song-api/songs", new CreateSongRequest(band.Id, "Song"))).Value;
    }
}
