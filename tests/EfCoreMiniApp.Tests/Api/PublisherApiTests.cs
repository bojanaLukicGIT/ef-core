using System.Net;
using System.Net.Http.Json;
using EfCoreMiniApp.Api.Dtos;
using EfCoreMiniApp.Domain;
using EfCoreMiniApp.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace EfCoreMiniApp.Tests.Api;

public class PublisherApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetPublishers_ReturnsPublishersWithBookCounts()
    {
        var publishers = await _client.GetFromJsonAsync<List<PublisherDto>>("/api/publishers", TestJson.Options);

        Assert.Equal(4, publishers!.Single(p => p.Name == "Blue Harbor Books").BookCount);
        Assert.Equal(0, publishers!.Single(p => p.Name == "Lantern House").BookCount);
    }

    [Fact]
    public async Task GetPublisherById_ReturnsPublisherOrNotFound()
    {
        var publisher = await _client.GetFromJsonAsync<PublisherDto>("/api/publishers/2", TestJson.Options);
        var missing = await _client.GetAsync("/api/publishers/9999");

        Assert.Equal("Northwind Publishing", publisher!.Name);
        Assert.Equal(1987, publisher.FoundedYear);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task PostPublisher_StoresComplexTypeAddressAndFiltersByCity()
    {
        var input = new PublisherInput
        {
            Name = "Danube Press",
            FoundedYear = 1999,
            Address = new AddressDto { Street = "Knez Mihailova 1", City = "Belgrade", PostalCode = "11000" }
        };

        var response = await _client.PostAsJsonAsync("/api/publishers", input, TestJson.Options);
        var created = await response.Content.ReadFromJsonAsync<PublisherDto>(TestJson.Options);
        var inBelgrade = await _client.GetFromJsonAsync<List<PublisherDto>>("/api/publishers?city=Belgrade", TestJson.Options);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"/api/publishers/{created!.PublisherId}", response.Headers.Location!.ToString());
        Assert.Equal("Belgrade", created.Address.City);
        Assert.Equal(["Danube Press"], inBelgrade!.Select(p => p.Name));
    }

    [Fact]
    public async Task PostPublisher_WithInvalidData_ReturnsValidationProblem()
    {
        var input = new PublisherInput
        {
            Name = "",
            FoundedYear = 1200,
            Address = new AddressDto { PostalCode = new string('9', 25) }
        };

        var response = await _client.PostAsJsonAsync("/api/publishers", input, TestJson.Options);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Name", problem!.Errors.Keys);
        Assert.Contains("FoundedYear", problem.Errors.Keys);
    }

    [Fact]
    public async Task PostPublisher_WithInvalidNestedAddress_ReturnsValidationProblem()
    {
        var input = new PublisherInput { Name = "Valid", Address = new AddressDto { PostalCode = new string('9', 25) } };

        var response = await _client.PostAsJsonAsync("/api/publishers", input, TestJson.Options);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Address.PostalCode", problem!.Errors.Keys);
    }

    [Fact]
    public async Task GetPublisherBooks_ReturnsRelatedBooks()
    {
        var books = await _client.GetFromJsonAsync<List<BookDto>>("/api/publishers/2/books", TestJson.Options);
        var missing = await _client.GetAsync("/api/publishers/9999/books");

        Assert.Equal(["Wool", "Shift", "A Tale For the Time Being"], books!.Select(b => b.Title));
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task DeletePublisher_KeepsBooksAndClearsOptionalForeignKey()
    {
        var publisherResponse = await _client.PostAsJsonAsync("/api/publishers", new PublisherInput { Name = "Short Lived Press" }, TestJson.Options);
        var publisher = await publisherResponse.Content.ReadFromJsonAsync<PublisherDto>(TestJson.Options);
        var bookResponse = await _client.PostAsJsonAsync("/api/books", new BookInput
        {
            Title = "Orphaned By Publisher",
            PublishDate = new DateOnly(2021, 1, 1),
            Genre = BookGenre.History,
            AuthorId = 7,
            PublisherId = publisher!.PublisherId
        }, TestJson.Options);
        var book = await bookResponse.Content.ReadFromJsonAsync<BookDetailsDto>(TestJson.Options);

        var response = await _client.DeleteAsync($"/api/publishers/{publisher.PublisherId}");
        var bookAfter = await _client.GetFromJsonAsync<BookDetailsDto>($"/api/books/{book!.BookId}", TestJson.Options);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null(bookAfter!.PublisherId);
        Assert.Null(bookAfter.PublisherName);
    }
}
