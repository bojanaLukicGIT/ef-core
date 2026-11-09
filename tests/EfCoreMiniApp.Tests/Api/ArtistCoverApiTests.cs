using System.Net;
using System.Net.Http.Json;
using EfCoreMiniApp.Api.Dtos;
using EfCoreMiniApp.Tests.Infrastructure;

namespace EfCoreMiniApp.Tests.Api;

public class ArtistCoverApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetArtists_CanReturnOnlyArtistsWithCovers()
    {
        var all = await _client.GetFromJsonAsync<List<ArtistDto>>("/api/artists", TestJson.Options);
        var withCovers = await _client.GetFromJsonAsync<List<ArtistDto>>("/api/artists?withCoversOnly=true", TestJson.Options);

        Assert.Contains(all!, a => a.LastName == "Talmage" && a.CoverCount == 0);
        Assert.DoesNotContain(withCovers!, a => a.LastName == "Talmage");
        Assert.All(withCovers!, a => Assert.True(a.CoverCount > 0));
    }

    [Fact]
    public async Task GetArtistById_IncludesCoversAndBooks()
    {
        var artist = await _client.GetFromJsonAsync<ArtistDetailsDto>("/api/artists/1", TestJson.Options);

        Assert.Equal("Picasso", artist!.LastName);
        Assert.Contains(artist.Covers, c => c.BookTitle == "The Left Hand of Darkness");
        Assert.Contains(artist.Covers, c => c.BookTitle == "In God's Ear");
    }

    [Fact]
    public async Task PostArtist_ValidatesAndCreates()
    {
        var invalid = await _client.PostAsJsonAsync("/api/artists", new ArtistInput { FirstName = "", LastName = "" }, TestJson.Options);
        var created = await _client.PostAsJsonAsync("/api/artists", new ArtistInput { FirstName = "Mary", LastName = "Blair" }, TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
    }

    [Fact]
    public async Task GetCovers_ReturnsArtistsWithJoinPayload()
    {
        var covers = await _client.GetFromJsonAsync<List<CoverDetailsDto>>("/api/covers", TestJson.Options);

        var coverOne = covers!.Single(c => c.CoverId == 1);
        Assert.Equal(["Dee Bell", "Pablo Picasso"], coverOne.Artists.Select(a => a.FullName));
        Assert.All(coverOne.Artists, a => Assert.Equal(new DateTime(2024, 1, 1), a.AssignedOn));
    }

    [Fact]
    public async Task AssignArtist_AddsLinkBetweenExistingEntities()
    {
        var artist = await CreateArtistAsync("Assign");

        var response = await _client.PostAsync($"/api/covers/2/artists/{artist.ArtistId}", null);
        var duplicate = await _client.PostAsync($"/api/covers/2/artists/{artist.ArtistId}", null);
        var cover = await _client.GetFromJsonAsync<CoverDetailsDto>("/api/covers/2", TestJson.Options);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var assignment = Assert.Single(cover!.Artists, a => a.ArtistId == artist.ArtistId);
        Assert.True(assignment.AssignedOn > new DateTime(2024, 1, 1));
    }

    [Fact]
    public async Task AssignArtist_ReturnsNotFoundForUnknownEntities()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PostAsync("/api/covers/9999/artists/1", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PostAsync("/api/covers/2/artists/9999", null)).StatusCode);
    }

    [Fact]
    public async Task UnassignArtist_RemovesOnlyTheLink()
    {
        var artist = await CreateArtistAsync("Unassign");
        await _client.PostAsync($"/api/covers/3/artists/{artist.ArtistId}", null);

        var response = await _client.DeleteAsync($"/api/covers/3/artists/{artist.ArtistId}");
        var again = await _client.DeleteAsync($"/api/covers/3/artists/{artist.ArtistId}");
        var cover = await _client.GetFromJsonAsync<CoverDetailsDto>("/api/covers/3", TestJson.Options);
        var artistAfter = await _client.GetAsync($"/api/artists/{artist.ArtistId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);
        Assert.DoesNotContain(cover!.Artists, a => a.ArtistId == artist.ArtistId);
        Assert.Equal(HttpStatusCode.OK, artistAfter.StatusCode);
    }

    [Fact]
    public async Task ReassignArtist_ReplacesTheLink()
    {
        var oldArtist = await CreateArtistAsync("OldArtist");
        var newArtist = await CreateArtistAsync("NewArtist");
        await _client.PostAsync($"/api/covers/4/artists/{oldArtist.ArtistId}", null);

        var response = await _client.PutAsJsonAsync(
            $"/api/covers/4/artists/{oldArtist.ArtistId}",
            new ReassignArtistInput { NewArtistId = newArtist.ArtistId },
            TestJson.Options);
        var cover = await _client.GetFromJsonAsync<CoverDetailsDto>("/api/covers/4", TestJson.Options);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Contains(cover!.Artists, a => a.ArtistId == newArtist.ArtistId);
        Assert.DoesNotContain(cover.Artists, a => a.ArtistId == oldArtist.ArtistId);
    }

    private async Task<ArtistDto> CreateArtistAsync(string lastName)
    {
        var response = await _client.PostAsJsonAsync("/api/artists", new ArtistInput { FirstName = "Test", LastName = lastName }, TestJson.Options);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ArtistDto>(TestJson.Options))!;
    }
}
