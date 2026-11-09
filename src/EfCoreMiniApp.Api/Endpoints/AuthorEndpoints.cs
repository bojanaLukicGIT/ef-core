using EfCoreMiniApp.Api.Dtos;
using EfCoreMiniApp.Api.Validation;
using EfCoreMiniApp.Data;
using EfCoreMiniApp.Data.Interceptors;
using EfCoreMiniApp.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace EfCoreMiniApp.Api.Endpoints;

public static class AuthorEndpoints
{
    public static void MapAuthorEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/authors").WithTags("Authors");

        group.MapGet("/", GetAuthors);
        group.MapGet("/{authorId:int}", GetAuthorById);
        group.MapPost("/", CreateAuthor).WithValidation<AuthorInput>();
        group.MapPut("/{authorId:int}", UpdateAuthor).WithValidation<AuthorInput>();
        group.MapDelete("/{authorId:int}", DeleteAuthor);
        group.MapGet("/{authorId:int}/books", GetAuthorBooks);
        group.MapGet("/{authorId:int}/graph", GetAuthorGraph);
        group.MapGet("/published-since/{year:int}", GetAuthorsPublishedSince);
    }

    private static async Task<Ok<PagedResult<AuthorDto>>> GetAuthors(
        PubContext db,
        string? lastNameStartsWith,
        int page = 1,
        int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Authors.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(lastNameStartsWith))
        {
            query = query.Where(a => EF.Functions.Like(a.LastName, lastNameStartsWith + "%"));
        }

        var totalCount = await query.CountAsync();
        var authors = await query
            .TagWith(QueryHintCommandInterceptor.RobustPlanTag)
            .OrderBy(a => a.LastName)
            .ThenBy(a => a.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuthorDto(a.AuthorId, a.FirstName, a.LastName, a.Books.Count))
            .ToListAsync();

        return TypedResults.Ok(new PagedResult<AuthorDto>(page, pageSize, totalCount, authors));
    }

    private static async Task<Results<Ok<AuthorDetailsDto>, NotFound>> GetAuthorById(int authorId, PubContext db)
    {
        var result = await db.Authors
            .AsNoTracking()
            .Where(a => a.AuthorId == authorId)
            .Select(a => new { Author = a, BookCount = a.Books.Count })
            .FirstOrDefaultAsync();

        return result is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(AuthorDetailsDto.From(result.Author, result.BookCount));
    }

    private static async Task<Created<AuthorDetailsDto>> CreateAuthor(AuthorInput input, PubContext db)
    {
        var author = new Author();
        input.ApplyTo(author);

        db.Authors.Add(author);
        await db.SaveChangesAsync();

        return TypedResults.Created($"/api/authors/{author.AuthorId}", AuthorDetailsDto.From(author, 0));
    }

    private static async Task<Results<Ok<AuthorDetailsDto>, NotFound>> UpdateAuthor(
        int authorId,
        AuthorInput input,
        PubContext db)
    {
        var author = await db.Authors.FindAsync(authorId);
        if (author is null)
        {
            return TypedResults.NotFound();
        }

        input.ApplyTo(author);
        await db.SaveChangesAsync();

        var bookCount = await db.Books.CountAsync(b => b.AuthorId == authorId);
        return TypedResults.Ok(AuthorDetailsDto.From(author, bookCount));
    }

    private static async Task<Results<NoContent, NotFound>> DeleteAuthor(int authorId, PubContext db)
    {
        var author = await db.Authors
            .Include(a => a.Books)
            .ThenInclude(b => b.Cover)
            .SingleOrDefaultAsync(a => a.AuthorId == authorId);

        if (author is null)
        {
            return TypedResults.NotFound();
        }

        db.Authors.Remove(author);
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<List<BookDto>>, NotFound>> GetAuthorBooks(int authorId, PubContext db)
    {
        var author = await db.Authors.FindAsync(authorId);
        if (author is null)
        {
            return TypedResults.NotFound();
        }

        await db.Entry(author)
            .Collection(a => a.Books)
            .Query()
            .Include(b => b.Publisher)
            .LoadAsync();

        var books = author.Books
            .OrderBy(b => b.PublishDate)
            .ThenBy(b => b.Title)
            .Select(BookDto.From)
            .ToList();

        return TypedResults.Ok(books);
    }

    private static async Task<Results<Ok<AuthorGraphDto>, NotFound>> GetAuthorGraph(int authorId, PubContext db)
    {
        var author = await db.Authors
            .AsNoTracking()
            .Include(a => a.Books)
            .ThenInclude(b => b.Cover!)
            .ThenInclude(c => c.Artists)
            .SingleOrDefaultAsync(a => a.AuthorId == authorId);

        if (author is null)
        {
            return TypedResults.NotFound();
        }

        var graph = new AuthorGraphDto(
            author.AuthorId,
            $"{author.FirstName} {author.LastName}",
            author.Books
                .OrderBy(b => b.Title)
                .Select(b => new AuthorGraphBookDto(
                    b.BookId,
                    b.Title,
                    b.Cover?.DesignIdeas,
                    b.Cover?.Artists.Select(artist => $"{artist.FirstName} {artist.LastName}").Order().ToList() ?? []))
                .ToList());

        return TypedResults.Ok(graph);
    }

    private static async Task<Ok<List<AuthorDto>>> GetAuthorsPublishedSince(int year, PubContext db)
    {
        var authors = await db.Authors
            .AsNoTracking()
            .Where(a => a.Books.Any(b => b.PublishDate.Year >= year))
            .OrderBy(a => a.LastName)
            .ThenBy(a => a.FirstName)
            .Select(a => new AuthorDto(a.AuthorId, a.FirstName, a.LastName, a.Books.Count))
            .ToListAsync();

        return TypedResults.Ok(authors);
    }
}
