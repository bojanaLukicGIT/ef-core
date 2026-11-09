using System.Net;
using System.Net.Http.Json;
using EfCoreMiniApp.Api.Dtos;
using EfCoreMiniApp.Domain;
using EfCoreMiniApp.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace EfCoreMiniApp.Tests.Api;

public class BookApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetBooks_FiltersByGenre()
    {
        var books = await _client.GetFromJsonAsync<List<BookDto>>("/api/books?genre=ScienceFiction", TestJson.Options);

        Assert.Contains(books!, b => b.Title == "The Left Hand of Darkness");
        Assert.All(books!, b => Assert.Equal(BookGenre.ScienceFiction, b.Genre));
    }

    [Fact]
    public async Task GetBookById_ReturnsDetailsWithCover()
    {
        var book = await _client.GetFromJsonAsync<BookDetailsDto>("/api/books/3", TestJson.Options);

        Assert.Equal("The Left Hand of Darkness", book!.Title);
        Assert.Equal("Ursula K. LeGuin", book.AuthorName);
        Assert.Equal("Blue Harbor Books", book.PublisherName);
        Assert.Equal("Black", book.PrimaryColor);
        Assert.Equal("How about a left hand in the dark?", book.Cover!.DesignIdeas);
    }

    [Fact]
    public async Task GetBookById_ReturnsNotFoundForUnknownId()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/books/9999")).StatusCode);
    }

    [Fact]
    public async Task Search_FindsBooksByTitleOrAuthor()
    {
        var byAuthor = await _client.GetFromJsonAsync<List<BookDto>>("/api/books/search?term=Howey", TestJson.Options);
        var byTitle = await _client.GetFromJsonAsync<List<BookDto>>("/api/books/search?term=Earthsea", TestJson.Options);

        Assert.Equal(["Shift", "Wool"], byAuthor!.Select(b => b.Title));
        Assert.Equal(["A Wizard of Earthsea"], byTitle!.Select(b => b.Title));
    }

    [Fact]
    public async Task Search_WithoutTerm_ReturnsBadRequest()
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync("/api/books/search")).StatusCode);
    }

    [Fact]
    public async Task SearchSql_ReturnsTheGeneratedQueryString()
    {
        var sql = await _client.GetStringAsync("/api/books/search/sql?term=Wool");

        Assert.Contains("SELECT", sql);
        Assert.Contains("FROM \"Books\"", sql);
        Assert.Contains("Wool", sql);
    }

    [Fact]
    public async Task FilterByAuthorAndPublisher_ReturnRelatedBooks()
    {
        var byAuthor = await _client.GetFromJsonAsync<List<BookDto>>("/api/books/by-author/1", TestJson.Options);
        var byPublisher = await _client.GetFromJsonAsync<List<BookDto>>("/api/books/by-publisher/2", TestJson.Options);
        var unknownPublisher = await _client.GetAsync("/api/books/by-publisher/9999");

        Assert.Equal(["Call Me Ishtar", "In God's Ear"], byAuthor!.Select(b => b.Title));
        Assert.Contains(byPublisher!, b => b.Title == "Wool");
        Assert.All(byPublisher!, b => Assert.Equal(2, b.PublisherId));
        Assert.Equal(HttpStatusCode.NotFound, unknownPublisher.StatusCode);
    }

    [Fact]
    public async Task Statistics_GroupsBooksByGenre()
    {
        var statistics = await _client.GetFromJsonAsync<BookStatisticsDto>("/api/books/statistics", TestJson.Options);

        Assert.Equal(statistics!.TotalBooks, statistics.ByGenre.Sum(g => g.BookCount));
        Assert.Contains(statistics.ByGenre, g => g.Genre == BookGenre.ScienceFiction);
        Assert.True(statistics.BooksWithCover >= 1);
    }

    [Fact]
    public async Task PostBook_CreatesBookWithConvertedValues()
    {
        var response = await _client.PostAsJsonAsync("/api/books", NewBook("Dune", primaryColor: "SandyBrown"), TestJson.Options);
        var created = await response.Content.ReadFromJsonAsync<BookDetailsDto>(TestJson.Options);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(BookGenre.ScienceFiction, created!.Genre);
        Assert.Equal("SandyBrown", created.PrimaryColor);
        Assert.Equal("Frank Herbert", created.AuthorName);
    }

    [Fact]
    public async Task PostBook_WithUnknownAuthorOrInvalidValues_ReturnsValidationProblem()
    {
        var unknownAuthor = await _client.PostAsJsonAsync("/api/books", NewBook("Ghost") with { AuthorId = 9999 }, TestJson.Options);
        var invalidValues = await _client.PostAsJsonAsync(
            "/api/books",
            NewBook("", primaryColor: "NotAColor") with { BasePrice = -1 },
            TestJson.Options);
        var invalidColor = await _client.PostAsJsonAsync("/api/books", NewBook("Colorless", primaryColor: "NotAColor"), TestJson.Options);

        var unknownAuthorProblem = await unknownAuthor.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJson.Options);
        var invalidValuesProblem = await invalidValues.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJson.Options);
        var invalidColorProblem = await invalidColor.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, unknownAuthor.StatusCode);
        Assert.Contains("AuthorId", unknownAuthorProblem!.Errors.Keys);
        Assert.Contains("Title", invalidValuesProblem!.Errors.Keys);
        Assert.Contains("BasePrice", invalidValuesProblem.Errors.Keys);
        Assert.Contains("PrimaryColor", invalidColorProblem!.Errors.Keys);
    }

    [Fact]
    public async Task PutBook_UpdatesValuesAndRelationships()
    {
        var created = await CreateBookAsync("Children of Dune");
        var update = new BookUpdateInput
        {
            Title = "Children of Dune",
            PublishDate = created.PublishDate,
            BasePrice = 21.5m,
            Genre = BookGenre.Adventure,
            AuthorId = 7,
            PublisherId = 2,
            RowVersion = created.RowVersion
        };

        var response = await _client.PutAsJsonAsync($"/api/books/{created.BookId}", update, TestJson.Options);
        var updated = await response.Content.ReadFromJsonAsync<BookDetailsDto>(TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(21.5m, updated!.BasePrice);
        Assert.Equal(BookGenre.Adventure, updated.Genre);
        Assert.Equal("Northwind Publishing", updated.PublisherName);
    }

    [Fact]
    public async Task DeleteBook_RemovesBookAndCover()
    {
        var created = await CreateBookAsync("God Emperor of Dune");
        await _client.PostAsJsonAsync($"/api/books/{created.BookId}/cover", new CoverInput { DesignIdeas = "A sandworm" }, TestJson.Options);

        var response = await _client.DeleteAsync($"/api/books/{created.BookId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/books/{created.BookId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/books/{created.BookId}/cover")).StatusCode);
    }

    [Fact]
    public async Task CoverEndpoints_AddReplaceAndRemoveOneToOneDependent()
    {
        var book = await CreateBookAsync("Heretics of Dune");

        var added = await _client.PostAsJsonAsync($"/api/books/{book.BookId}/cover", new CoverInput { DesignIdeas = "First" }, TestJson.Options);
        var addedAgain = await _client.PostAsJsonAsync($"/api/books/{book.BookId}/cover", new CoverInput { DesignIdeas = "Again" }, TestJson.Options);
        var replaced = await _client.PutAsJsonAsync($"/api/books/{book.BookId}/cover", new CoverInput { DesignIdeas = "Second", DigitalOnly = true }, TestJson.Options);
        var current = await _client.GetFromJsonAsync<CoverDto>($"/api/books/{book.BookId}/cover", TestJson.Options);
        var removed = await _client.DeleteAsync($"/api/books/{book.BookId}/cover");
        var afterRemoval = await _client.GetAsync($"/api/books/{book.BookId}/cover");
        var bookAfterRemoval = await _client.GetFromJsonAsync<BookDetailsDto>($"/api/books/{book.BookId}", TestJson.Options);

        var firstCover = await added.Content.ReadFromJsonAsync<CoverDto>(TestJson.Options);
        Assert.Equal(HttpStatusCode.Created, added.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, addedAgain.StatusCode);
        Assert.Equal(HttpStatusCode.OK, replaced.StatusCode);
        Assert.NotEqual(firstCover!.CoverId, current!.CoverId);
        Assert.Equal("Second", current.DesignIdeas);
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, afterRemoval.StatusCode);
        Assert.Null(bookAfterRemoval!.Cover);
    }

    [Fact]
    public async Task CancelBook_CommitsNotesAndDeleteInOneTransaction()
    {
        var book = await CreateBookAsync("Chapterhouse: Dune");
        var artist = await CreateArtistAsync("Cancel", "Commit", notes: null);
        var cover = await AddCoverAsync(book.BookId);
        await _client.PostAsync($"/api/covers/{cover.CoverId}/artists/{artist.ArtistId}", null);

        var response = await _client.PostAsync($"/api/books/{book.BookId}/cancel", null);
        var result = await response.Content.ReadFromJsonAsync<BookCancellationDto>(TestJson.Options);
        var artistAfter = await _client.GetFromJsonAsync<ArtistDetailsDto>($"/api/artists/{artist.ArtistId}", TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, result!.ArtistsNotified);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/books/{book.BookId}")).StatusCode);
        Assert.Contains($"Assigned book {book.BookId} was cancelled", artistAfter!.Notes);
        Assert.Empty(artistAfter.Covers);
    }

    [Fact]
    public async Task CancelBook_RollsBackWhenSavingNotesFails()
    {
        var book = await CreateBookAsync("Dune Messiah");
        var longNotes = new string('n', 490);
        var artist = await CreateArtistAsync("Cancel", "Rollback", longNotes);
        var cover = await AddCoverAsync(book.BookId);
        await _client.PostAsync($"/api/covers/{cover.CoverId}/artists/{artist.ArtistId}", null);

        var response = await _client.PostAsync($"/api/books/{book.BookId}/cancel", null);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestJson.Options);
        var artistAfter = await _client.GetFromJsonAsync<ArtistDetailsDto>($"/api/artists/{artist.ArtistId}", TestJson.Options);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Book cancellation was rolled back", problem!.Title);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/api/books/{book.BookId}")).StatusCode);
        Assert.Equal(longNotes, artistAfter!.Notes);
        Assert.Single(artistAfter.Covers);
    }

    private static BookInput NewBook(string title, string? primaryColor = null) => new()
    {
        Title = title,
        PublishDate = new DateOnly(1965, 8, 1),
        BasePrice = 19.99m,
        Genre = BookGenre.ScienceFiction,
        PrimaryColor = primaryColor,
        AuthorId = 7,
        PublisherId = 1
    };

    private async Task<BookDetailsDto> CreateBookAsync(string title)
    {
        var response = await _client.PostAsJsonAsync("/api/books", NewBook(title), TestJson.Options);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BookDetailsDto>(TestJson.Options))!;
    }

    private async Task<ArtistDto> CreateArtistAsync(string firstName, string lastName, string? notes)
    {
        var response = await _client.PostAsJsonAsync("/api/artists", new ArtistInput { FirstName = firstName, LastName = lastName, Notes = notes }, TestJson.Options);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ArtistDto>(TestJson.Options))!;
    }

    private async Task<CoverDto> AddCoverAsync(int bookId)
    {
        var response = await _client.PostAsJsonAsync($"/api/books/{bookId}/cover", new CoverInput { DesignIdeas = "Cover for cancellation" }, TestJson.Options);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CoverDto>(TestJson.Options))!;
    }
}
