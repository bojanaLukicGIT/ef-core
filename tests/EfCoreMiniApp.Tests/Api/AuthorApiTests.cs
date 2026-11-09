using System.Net;
using System.Net.Http.Json;
using EfCoreMiniApp.Api.Dtos;
using EfCoreMiniApp.Domain;
using EfCoreMiniApp.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace EfCoreMiniApp.Tests.Api;

public class AuthorApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetAuthors_ReturnsSortedPage()
    {
        var page = await _client.GetFromJsonAsync<PagedResult<AuthorDto>>("/api/authors?page=1&pageSize=3", TestJson.Options);

        Assert.Equal(3, page!.Items.Count);
        Assert.True(page.TotalCount >= 7);
        Assert.Equal("Allende", page.Items[0].LastName);
    }

    [Fact]
    public async Task GetAuthors_FiltersByLastNamePrefix()
    {
        var page = await _client.GetFromJsonAsync<PagedResult<AuthorDto>>("/api/authors?lastNameStartsWith=Le", TestJson.Options);

        Assert.Equal(["LeGuin", "Lerman"], page!.Items.Select(a => a.LastName));
    }

    [Fact]
    public async Task GetAuthorById_ReturnsDetailsWithJsonMappedData()
    {
        var author = await _client.GetFromJsonAsync<AuthorDetailsDto>("/api/authors/4", TestJson.Options);

        Assert.Equal("LeGuin", author!.LastName);
        Assert.Equal(["UKL"], author.Nicknames);
        Assert.Equal(2, author.BookCount);
    }

    [Fact]
    public async Task GetAuthorById_ReturnsNotFoundForUnknownId()
    {
        var response = await _client.GetAsync("/api/authors/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostAuthor_CreatesAuthorAndReturnsLocation()
    {
        var input = new AuthorInput
        {
            FirstName = "William",
            LastName = "Shakespeare",
            Contact = new ContactDetailsDto { EmailAddress = "will@upstart.crow", Phone = "555-555-1234" },
            Nicknames = ["The Bard", "Swan of Avon"]
        };

        var response = await _client.PostAsJsonAsync("/api/authors", input, TestJson.Options);
        var created = await response.Content.ReadFromJsonAsync<AuthorDetailsDto>(TestJson.Options);
        var fetched = await _client.GetFromJsonAsync<AuthorDetailsDto>(response.Headers.Location, TestJson.Options);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"/api/authors/{created!.AuthorId}", response.Headers.Location!.ToString());
        Assert.Equal("will@upstart.crow", fetched!.Contact!.EmailAddress);
        Assert.Equal(["The Bard", "Swan of Avon"], fetched.Nicknames);
    }

    [Fact]
    public async Task PostAuthor_WithInvalidData_ReturnsValidationProblem()
    {
        var input = new AuthorInput
        {
            FirstName = "",
            LastName = new string('x', 101),
            Contact = new ContactDetailsDto { EmailAddress = "not-an-email" }
        };

        var response = await _client.PostAsJsonAsync("/api/authors", input, TestJson.Options);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("FirstName", problem!.Errors.Keys);
        Assert.Contains("LastName", problem.Errors.Keys);
    }

    [Fact]
    public async Task PostAuthor_WithInvalidNestedContact_ReturnsValidationProblem()
    {
        var input = new AuthorInput
        {
            FirstName = "Valid",
            LastName = "Name",
            Contact = new ContactDetailsDto { EmailAddress = "not-an-email" }
        };

        var response = await _client.PostAsJsonAsync("/api/authors", input, TestJson.Options);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Contact.EmailAddress", problem!.Errors.Keys);
    }

    [Fact]
    public async Task PutAuthor_UpdatesScalarJsonAndCollectionData()
    {
        var created = await CreateAuthorAsync("Lynda", "Rutledge");
        var update = new AuthorInput
        {
            FirstName = "Lynda",
            LastName = "Rutledge-Smith",
            Contact = new ContactDetailsDto { EmailAddress = "lynda@example.com" },
            Nicknames = ["Giraffe Writer"]
        };

        var response = await _client.PutAsJsonAsync($"/api/authors/{created.AuthorId}", update, TestJson.Options);
        var fetched = await _client.GetFromJsonAsync<AuthorDetailsDto>($"/api/authors/{created.AuthorId}", TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Rutledge-Smith", fetched!.LastName);
        Assert.Equal("lynda@example.com", fetched.Contact!.EmailAddress);
        Assert.Equal(["Giraffe Writer"], fetched.Nicknames);
    }

    [Fact]
    public async Task PutAuthor_ReturnsNotFoundForUnknownId()
    {
        var response = await _client.PutAsJsonAsync("/api/authors/9999", new AuthorInput { FirstName = "A", LastName = "B" }, TestJson.Options);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAuthor_RemovesAuthorWithBooksAndCovers()
    {
        var author = await CreateAuthorAsync("Temporary", "Author");
        var bookResponse = await _client.PostAsJsonAsync("/api/books", new BookInput
        {
            Title = "Temporary Book",
            PublishDate = new DateOnly(2020, 1, 1),
            Genre = BookGenre.History,
            AuthorId = author.AuthorId
        }, TestJson.Options);
        var book = await bookResponse.Content.ReadFromJsonAsync<BookDetailsDto>(TestJson.Options);
        await _client.PostAsJsonAsync($"/api/books/{book!.BookId}/cover", new CoverInput { DesignIdeas = "Temporary cover" }, TestJson.Options);

        var response = await _client.DeleteAsync($"/api/authors/{author.AuthorId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/authors/{author.AuthorId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/books/{book.BookId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync($"/api/authors/{author.AuthorId}")).StatusCode);
    }

    [Fact]
    public async Task GetAuthorBooks_ReturnsRelatedBooks()
    {
        var books = await _client.GetFromJsonAsync<List<BookDto>>("/api/authors/5/books", TestJson.Options);

        Assert.Equal(["Wool", "Shift"], books!.Select(b => b.Title));
        Assert.All(books!, b => Assert.Equal("Northwind Publishing", b.PublisherName));
    }

    [Fact]
    public async Task GetAuthorGraph_ReturnsBooksCoversAndArtists()
    {
        var graph = await _client.GetFromJsonAsync<AuthorGraphDto>("/api/authors/4/graph", TestJson.Options);

        var leftHand = graph!.Books.Single(b => b.Title == "The Left Hand of Darkness");
        Assert.Equal("Ursula K. LeGuin", graph.FullName);
        Assert.Equal(["Dee Bell", "Pablo Picasso"], leftHand.CoverArtists);
        Assert.Null(graph.Books.Single(b => b.Title == "A Wizard of Earthsea").CoverDesignIdeas);
    }

    [Fact]
    public async Task GetAuthorsPublishedSince_FiltersUsingRelatedBooks()
    {
        var authors = await _client.GetFromJsonAsync<List<AuthorDto>>("/api/authors/published-since/2015", TestJson.Options);

        Assert.Contains(authors!, a => a.LastName == "Segovia");
        Assert.DoesNotContain(authors!, a => a.LastName == "Howey");
    }

    private async Task<AuthorDetailsDto> CreateAuthorAsync(string firstName, string lastName)
    {
        var response = await _client.PostAsJsonAsync("/api/authors", new AuthorInput { FirstName = firstName, LastName = lastName }, TestJson.Options);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthorDetailsDto>(TestJson.Options))!;
    }
}
