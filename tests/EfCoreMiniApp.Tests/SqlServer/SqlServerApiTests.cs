using System.Net;
using System.Net.Http.Json;
using EfCoreMiniApp.Api.Dtos;
using EfCoreMiniApp.Domain;
using EfCoreMiniApp.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace EfCoreMiniApp.Tests.SqlServer;

[Collection(SqlServerCollection.Name)]
[Trait("Category", "SqlServer")]
public class SqlServerApiTests : IDisposable
{
    private readonly SqlServerApiFactory _factory = new();
    private readonly HttpClient _client;

    public SqlServerApiTests(SqlServerDatabaseFixture fixture)
    {
        _ = fixture;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task PutWithStaleRowVersion_ReturnsConflict()
    {
        var created = await CreateBookAsync("Concurrency Through The Api");
        var update = new BookUpdateInput
        {
            Title = created.Title,
            PublishDate = created.PublishDate,
            BasePrice = 20m,
            Genre = created.Genre,
            AuthorId = created.AuthorId,
            RowVersion = created.RowVersion
        };

        var first = await _client.PutAsJsonAsync($"/api/books/{created.BookId}", update, TestJson.Options);
        var second = await _client.PutAsJsonAsync($"/api/books/{created.BookId}", update with { BasePrice = 25m }, TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var problem = await second.Content.ReadFromJsonAsync<ProblemDetails>(TestJson.Options);
        Assert.Equal("Concurrency conflict", problem!.Title);
        Assert.True(problem.Extensions.ContainsKey("currentRowVersion"));
    }

    [Fact]
    public async Task StoredProcedureAndViewEndpoints_ReturnData()
    {
        var authors = await _client.GetFromJsonAsync<List<AuthorSummaryDto>>("/api/sql/authors/published-between?startYear=2010&endYear=2015", TestJson.Options);
        var names = await _client.GetFromJsonAsync<List<AuthorNameDto>>("/api/sql/authors/names", TestJson.Options);
        var rows = await _client.GetFromJsonAsync<List<AuthorByArtistDto>>("/api/sql/authors-by-artist?artistStartsWith=Kir", TestJson.Options);
        var count = await _client.GetFromJsonAsync<int>("/api/sql/books/count", TestJson.Options);
        var titles = await _client.GetFromJsonAsync<List<string>>("/api/sql/books/titles?minLength=20", TestJson.Options);
        var fromSql = await _client.GetFromJsonAsync<List<AuthorDto>>("/api/sql/authors/from-sql?lastNameStart=L", TestJson.Options);
        var fromSqlRaw = await _client.GetFromJsonAsync<List<AuthorDto>>("/api/sql/authors/from-sql-raw?lastNameStart=O", TestJson.Options);

        Assert.Equal(["Howey", "Ozeki"], authors!.Select(a => a.LastName));
        Assert.Equal(7, names!.Count);
        Assert.Equal([new AuthorByArtistDto("Kir Talmage", null)], rows);
        Assert.True(count >= 9);
        Assert.Contains("The Left Hand of Darkness", titles!);
        Assert.Equal(["LeGuin", "Lerman"], fromSql!.Select(a => a.LastName));
        Assert.Equal(["Ozeki"], fromSqlRaw!.Select(a => a.LastName));
    }

    [Fact]
    public async Task CoverEndpoints_WorkWithStoredProcedureMapping()
    {
        var book = await CreateBookAsync("Cover Lifecycle On Sql Server");

        var added = await _client.PostAsJsonAsync($"/api/books/{book.BookId}/cover", new CoverInput { DesignIdeas = "First" }, TestJson.Options);
        var addedCover = await added.Content.ReadFromJsonAsync<CoverDto>(TestJson.Options);
        var replaced = await _client.PutAsJsonAsync($"/api/books/{book.BookId}/cover", new CoverInput { DesignIdeas = "Second", DigitalOnly = true }, TestJson.Options);
        var replacedCover = await replaced.Content.ReadFromJsonAsync<CoverDto>(TestJson.Options);
        var removed = await _client.DeleteAsync($"/api/books/{book.BookId}/cover");
        var afterRemoval = await _client.GetAsync($"/api/books/{book.BookId}/cover");

        Assert.Equal(HttpStatusCode.Created, added.StatusCode);
        Assert.Equal(HttpStatusCode.OK, replaced.StatusCode);
        Assert.NotEqual(addedCover!.CoverId, replacedCover!.CoverId);
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, afterRemoval.StatusCode);
    }

    [Fact]
    public async Task DeleteCoverThroughExecuteSql_Returns204ThenNotFound()
    {
        var book = await CreateBookAsync("Raw Delete Cover");
        var added = await _client.PostAsJsonAsync($"/api/books/{book.BookId}/cover", new CoverInput { DesignIdeas = "Disposable" }, TestJson.Options);
        var cover = await added.Content.ReadFromJsonAsync<CoverDto>(TestJson.Options);

        var first = await _client.DeleteAsync($"/api/sql/covers/{cover!.CoverId}");
        var second = await _client.DeleteAsync($"/api/sql/covers/{cover.CoverId}");

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
    }

    [Fact]
    public async Task GetAuthors_UsesTheTaggedQueryWithTheQueryHint()
    {
        var response = await _client.GetFromJsonAsync<PagedResult<AuthorDto>>("/api/authors?pageSize=3", TestJson.Options);

        Assert.Equal(3, response!.Items.Count);
        Assert.Equal(7, response.TotalCount);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<BookDetailsDto> CreateBookAsync(string title)
    {
        var input = new BookInput
        {
            Title = title,
            PublishDate = new DateOnly(2022, 6, 1),
            BasePrice = 10m,
            Genre = BookGenre.History,
            AuthorId = 7,
            PublisherId = 3
        };

        var response = await _client.PostAsJsonAsync("/api/books", input, TestJson.Options);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BookDetailsDto>(TestJson.Options))!;
    }
}
