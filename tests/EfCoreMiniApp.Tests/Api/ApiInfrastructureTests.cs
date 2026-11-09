using System.Net;
using EfCoreMiniApp.Tests.Infrastructure;

namespace EfCoreMiniApp.Tests.Api;

public class ApiInfrastructureTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task SwaggerDocument_DescribesTheEndpoints()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        var document = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("/api/authors", document);
        Assert.Contains("/api/books/{bookId}/cover", document);
        Assert.Contains("/api/covers/{coverId}/artists/{artistId}", document);
        Assert.Contains("/api/sql/authors-by-artist", document);
    }

    [Fact]
    public async Task UnknownRoute_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/unknown");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MalformedJson_ReturnsBadRequest()
    {
        var content = new StringContent("{ not json", System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/authors", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
