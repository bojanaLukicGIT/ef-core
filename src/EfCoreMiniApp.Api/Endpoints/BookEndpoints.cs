using System.ComponentModel.DataAnnotations;
using EfCoreMiniApp.Api.Dtos;
using EfCoreMiniApp.Api.Validation;
using EfCoreMiniApp.Data;
using EfCoreMiniApp.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EfCoreMiniApp.Api.Endpoints;

public static class BookEndpoints
{
    public static void MapBookEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/books").WithTags("Books");

        group.MapGet("/", GetBooks);
        group.MapGet("/{bookId:int}", GetBookById);
        group.MapGet("/search", SearchBooks);
        group.MapGet("/search/sql", GetSearchSql);
        group.MapGet("/by-author/{authorId:int}", GetBooksByAuthor);
        group.MapGet("/by-publisher/{publisherId:int}", GetBooksByPublisher);
        group.MapGet("/statistics", GetStatistics);
        group.MapPost("/", CreateBook).WithValidation<BookInput>();
        group.MapPut("/{bookId:int}", UpdateBook).WithValidation<BookUpdateInput>();
        group.MapDelete("/{bookId:int}", DeleteBook);
        group.MapPost("/{bookId:int}/cancel", CancelBook);

        group.MapGet("/{bookId:int}/cover", GetCover);
        group.MapPost("/{bookId:int}/cover", AddCover).WithValidation<CoverInput>();
        group.MapPut("/{bookId:int}/cover", ReplaceCover).WithValidation<CoverInput>();
        group.MapDelete("/{bookId:int}/cover", RemoveCover);
    }

    private static async Task<Ok<List<BookDto>>> GetBooks(PubContext db, BookGenre? genre)
    {
        var query = db.Books.AsNoTracking();
        if (genre is not null)
        {
            query = query.Where(b => b.Genre == genre);
        }

        var books = await query
            .OrderByDescending(b => b.PublishDate)
            .ThenBy(b => b.Title)
            .Select(BookDto.Projection)
            .ToListAsync();

        return TypedResults.Ok(books);
    }

    private static async Task<Results<Ok<BookDetailsDto>, NotFound>> GetBookById(int bookId, PubContext db)
    {
        var book = await FindDetailsAsync(db, bookId);
        return book is null ? TypedResults.NotFound() : TypedResults.Ok(book);
    }

    private static async Task<Results<Ok<List<BookDto>>, ValidationProblem>> SearchBooks(string? term, PubContext db)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["term"] = ["A search term is required."]
            });
        }

        var books = await BuildSearchQuery(db, term)
            .Select(BookDto.Projection)
            .ToListAsync();

        return TypedResults.Ok(books);
    }

    private static Results<ContentHttpResult, ValidationProblem> GetSearchSql(string? term, PubContext db)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["term"] = ["A search term is required."]
            });
        }

        return TypedResults.Text(BuildSearchQuery(db, term).ToQueryString());
    }

    private static async Task<Results<Ok<List<BookDto>>, NotFound>> GetBooksByAuthor(int authorId, PubContext db)
    {
        if (!await db.Authors.AnyAsync(a => a.AuthorId == authorId))
        {
            return TypedResults.NotFound();
        }

        var books = await db.Books
            .AsNoTracking()
            .Where(b => b.AuthorId == authorId)
            .OrderBy(b => b.PublishDate)
            .Select(BookDto.Projection)
            .ToListAsync();

        return TypedResults.Ok(books);
    }

    private static async Task<Results<Ok<List<BookDto>>, NotFound>> GetBooksByPublisher(int publisherId, PubContext db)
    {
        if (!await db.Publishers.AnyAsync(p => p.PublisherId == publisherId))
        {
            return TypedResults.NotFound();
        }

        var books = await db.Books
            .AsNoTracking()
            .Where(b => b.PublisherId == publisherId)
            .OrderBy(b => b.Title)
            .Select(BookDto.Projection)
            .ToListAsync();

        return TypedResults.Ok(books);
    }

    private static async Task<Ok<BookStatisticsDto>> GetStatistics(PubContext db)
    {
        var totalBooks = await db.Books.CountAsync();
        var booksWithCover = await db.Books.CountAsync(b => b.Cover != null);
        var booksWithoutPublisher = await db.Books.CountAsync(b => b.PublisherId == null);
        var byGenre = await db.Books
            .GroupBy(b => b.Genre)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .Select(g => new GenreStatisticsDto(g.Key, g.Count()))
            .ToListAsync();

        return TypedResults.Ok(new BookStatisticsDto(totalBooks, booksWithCover, booksWithoutPublisher, byGenre));
    }

    private static async Task<Results<Created<BookDetailsDto>, ValidationProblem>> CreateBook(BookInput input, PubContext db)
    {
        var errors = await ValidateRelationshipsAsync(input, db);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var book = new Book();
        input.ApplyTo(book);

        db.Books.Add(book);
        await db.SaveChangesAsync();

        var created = await FindDetailsAsync(db, book.BookId);
        return TypedResults.Created($"/api/books/{book.BookId}", created);
    }

    private static async Task<Results<Ok<BookDetailsDto>, NotFound, ValidationProblem, Conflict<ProblemDetails>>> UpdateBook(
        int bookId,
        BookUpdateInput input,
        PubContext db)
    {
        var book = await db.Books.FindAsync(bookId);
        if (book is null)
        {
            return TypedResults.NotFound();
        }

        var errors = await ValidateRelationshipsAsync(input, db);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        db.Entry(book).Property(b => b.RowVersion).OriginalValue = input.RowVersion;
        input.ApplyTo(book);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException exception)
        {
            var databaseValues = await exception.Entries.Single().GetDatabaseValuesAsync();
            var problem = new ProblemDetails
            {
                Title = "Concurrency conflict",
                Detail = databaseValues is null
                    ? "The book was deleted by another user."
                    : "The book was changed by another user. Reload it and try again.",
                Status = StatusCodes.Status409Conflict
            };
            problem.Extensions["currentRowVersion"] = databaseValues?.GetValue<byte[]?>(nameof(Book.RowVersion));
            return TypedResults.Conflict(problem);
        }

        var updated = await FindDetailsAsync(db, bookId);
        return TypedResults.Ok(updated!);
    }

    private static async Task<Results<NoContent, NotFound>> DeleteBook(int bookId, PubContext db)
    {
        var book = await db.Books
            .Include(b => b.Cover)
            .SingleOrDefaultAsync(b => b.BookId == bookId);

        if (book is null)
        {
            return TypedResults.NotFound();
        }

        db.Books.Remove(book);
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<BookCancellationDto>, NotFound, ProblemHttpResult>> CancelBook(int bookId, PubContext db)
    {
        if (!await db.Books.AnyAsync(b => b.BookId == bookId))
        {
            return TypedResults.NotFound();
        }

        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var artists = await db.Artists
                .Where(a => a.Covers.Any(c => c.BookId == bookId))
                .ToListAsync();

            foreach (var artist in artists)
            {
                var note = $"Assigned book {bookId} was cancelled on {DateTime.UtcNow:yyyy-MM-dd}.";
                artist.Notes = string.IsNullOrEmpty(artist.Notes) ? note : $"{artist.Notes}{Environment.NewLine}{note}";
            }

            await db.Books.Where(b => b.BookId == bookId).ExecuteDeleteAsync();
            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return TypedResults.Ok(new BookCancellationDto(bookId, artists.Count));
        }
        catch (Exception exception) when (exception is DbUpdateException or ValidationException)
        {
            await transaction.RollbackAsync();
            db.ChangeTracker.Clear();

            return TypedResults.Problem(
                title: "Book cancellation was rolled back",
                detail: exception.Message,
                statusCode: StatusCodes.Status409Conflict);
        }
    }

    private static async Task<Results<Ok<CoverDto>, NotFound>> GetCover(int bookId, PubContext db)
    {
        var cover = await db.Covers
            .AsNoTracking()
            .Where(c => c.BookId == bookId)
            .Select(c => new CoverDto(c.CoverId, c.DesignIdeas, c.DigitalOnly, c.BookId))
            .SingleOrDefaultAsync();

        return cover is null ? TypedResults.NotFound() : TypedResults.Ok(cover);
    }

    private static async Task<Results<Created<CoverDto>, NotFound, Conflict<ProblemDetails>>> AddCover(
        int bookId,
        CoverInput input,
        PubContext db)
    {
        var book = await db.Books
            .Include(b => b.Cover)
            .SingleOrDefaultAsync(b => b.BookId == bookId);

        if (book is null)
        {
            return TypedResults.NotFound();
        }

        if (book.Cover is not null)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title = "The book already has a cover",
                Detail = "Use PUT to replace the existing cover.",
                Status = StatusCodes.Status409Conflict
            });
        }

        book.Cover = new Cover { DesignIdeas = input.DesignIdeas, DigitalOnly = input.DigitalOnly };
        await db.SaveChangesAsync();

        return TypedResults.Created($"/api/books/{bookId}/cover", ToCoverDto(book.Cover));
    }

    private static async Task<Results<Ok<CoverDto>, NotFound>> ReplaceCover(int bookId, CoverInput input, PubContext db)
    {
        var book = await db.Books
            .Include(b => b.Cover)
            .SingleOrDefaultAsync(b => b.BookId == bookId);

        if (book is null)
        {
            return TypedResults.NotFound();
        }

        book.Cover = new Cover { DesignIdeas = input.DesignIdeas, DigitalOnly = input.DigitalOnly };
        await db.SaveChangesAsync();

        return TypedResults.Ok(ToCoverDto(book.Cover));
    }

    private static async Task<Results<NoContent, NotFound>> RemoveCover(int bookId, PubContext db)
    {
        var book = await db.Books
            .Include(b => b.Cover)
            .SingleOrDefaultAsync(b => b.BookId == bookId);

        if (book?.Cover is null)
        {
            return TypedResults.NotFound();
        }

        book.Cover = null;
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    private static IQueryable<Book> BuildSearchQuery(PubContext db, string term) =>
        db.Books
            .AsNoTracking()
            .Where(b => b.Title.Contains(term)
                || b.Author.FirstName.Contains(term)
                || b.Author.LastName.Contains(term))
            .OrderBy(b => b.Title);

    private static Task<BookDetailsDto?> FindDetailsAsync(PubContext db, int bookId) =>
        db.Books
            .AsNoTracking()
            .Where(b => b.BookId == bookId)
            .Select(BookDetailsDto.Projection)
            .SingleOrDefaultAsync();

    private static async Task<Dictionary<string, string[]>> ValidateRelationshipsAsync(BookInput input, PubContext db)
    {
        var errors = new Dictionary<string, string[]>();

        if (!await db.Authors.AnyAsync(a => a.AuthorId == input.AuthorId))
        {
            errors[nameof(BookInput.AuthorId)] = [$"Author {input.AuthorId} does not exist."];
        }

        if (input.PublisherId is int publisherId && !await db.Publishers.AnyAsync(p => p.PublisherId == publisherId))
        {
            errors[nameof(BookInput.PublisherId)] = [$"Publisher {publisherId} does not exist."];
        }

        return errors;
    }

    private static CoverDto ToCoverDto(Cover cover) =>
        new(cover.CoverId, cover.DesignIdeas, cover.DigitalOnly, cover.BookId);
}
