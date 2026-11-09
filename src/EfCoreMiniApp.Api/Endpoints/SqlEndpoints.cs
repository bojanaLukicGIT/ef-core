using EfCoreMiniApp.Api.Dtos;
using EfCoreMiniApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace EfCoreMiniApp.Api.Endpoints;

public static class SqlEndpoints
{
    public static void MapSqlEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/sql").WithTags("Raw SQL, Views and Stored Procedures");

        group.MapGet("/authors/from-sql", GetAuthorsWithFromSql);
        group.MapGet("/authors/from-sql-raw", GetAuthorsWithFromSqlRaw);
        group.MapGet("/authors/published-between", GetAuthorsPublishedBetween);
        group.MapGet("/authors/names", GetAuthorNames);
        group.MapGet("/authors-by-artist", GetAuthorsByArtist);
        group.MapGet("/books/titles", GetLongBookTitles);
        group.MapGet("/books/count", GetBookCount);
        group.MapDelete("/covers/{coverId:int}", DeleteCover);
    }

    private static async Task<Ok<List<AuthorDto>>> GetAuthorsWithFromSql(PubContext db, string lastNameStart = "L")
    {
        var pattern = lastNameStart + "%";
        var authors = await db.Authors
            .FromSql($"SELECT * FROM Authors WHERE LastName LIKE {pattern}")
            .TagWith("FromSql interpolated and parameterized")
            .OrderBy(a => a.LastName)
            .Select(a => new AuthorDto(a.AuthorId, a.FirstName, a.LastName, a.Books.Count))
            .ToListAsync();

        return TypedResults.Ok(authors);
    }

    private static async Task<Ok<List<AuthorDto>>> GetAuthorsWithFromSqlRaw(PubContext db, string lastNameStart = "L")
    {
        var authors = await db.Authors
            .FromSqlRaw("SELECT * FROM Authors WHERE LastName LIKE {0}", lastNameStart + "%")
            .TagWith("FromSqlRaw with a positional parameter")
            .OrderBy(a => a.LastName)
            .Select(a => new AuthorDto(a.AuthorId, a.FirstName, a.LastName, a.Books.Count))
            .ToListAsync();

        return TypedResults.Ok(authors);
    }

    private static async Task<Ok<List<AuthorSummaryDto>>> GetAuthorsPublishedBetween(
        PubContext db,
        int startYear = 2010,
        int endYear = 2015)
    {
        var authors = await db.Database
            .SqlQuery<AuthorSummary>($"EXEC dbo.AuthorsPublishedinYearRange {startYear}, {endYear}")
            .ToListAsync();

        return TypedResults.Ok(authors
            .OrderBy(a => a.LastName)
            .Select(a => new AuthorSummaryDto(a.AuthorId, a.FirstName, a.LastName))
            .ToList());
    }

    private static async Task<Ok<List<AuthorNameDto>>> GetAuthorNames(PubContext db)
    {
        var names = await db.Database
            .SqlQuery<AuthorName>($"EXEC dbo.GetAuthorNames")
            .ToListAsync();

        return TypedResults.Ok(names
            .OrderBy(n => n.LastName)
            .Select(n => new AuthorNameDto(n.FirstName, n.LastName))
            .ToList());
    }

    private static async Task<Ok<List<AuthorByArtistDto>>> GetAuthorsByArtist(PubContext db, string? artistStartsWith)
    {
        var query = db.AuthorsByArtist.AsQueryable();
        if (!string.IsNullOrWhiteSpace(artistStartsWith))
        {
            query = query.Where(a => a.Artist.StartsWith(artistStartsWith));
        }

        var rows = await query
            .OrderBy(a => a.Artist)
            .ThenBy(a => a.Author)
            .Select(a => new AuthorByArtistDto(a.Artist, a.Author))
            .ToListAsync();

        return TypedResults.Ok(rows);
    }

    private static async Task<Ok<List<string>>> GetLongBookTitles(PubContext db, int minLength = 10)
    {
        var titles = await db.Database
            .SqlQuery<string>($"SELECT BookTitle AS Value FROM Books WHERE LEN(BookTitle) > {minLength}")
            .OrderBy(title => title)
            .ToListAsync();

        return TypedResults.Ok(titles);
    }

    private static async Task<Ok<int>> GetBookCount(PubContext db)
    {
        var count = await db.Database
            .SqlQuery<int>($"SELECT COUNT(*) AS Value FROM Books")
            .SingleAsync();

        return TypedResults.Ok(count);
    }

    private static async Task<Results<NoContent, NotFound>> DeleteCover(int coverId, PubContext db)
    {
        var affectedRows = await db.Database.ExecuteSqlAsync($"EXEC dbo.DeleteCover {coverId}");
        return affectedRows > 0 ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
