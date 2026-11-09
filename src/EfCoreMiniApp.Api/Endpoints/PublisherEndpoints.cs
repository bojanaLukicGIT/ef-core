using EfCoreMiniApp.Api.Dtos;
using EfCoreMiniApp.Api.Validation;
using EfCoreMiniApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace EfCoreMiniApp.Api.Endpoints;

public static class PublisherEndpoints
{
    public static void MapPublisherEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/publishers").WithTags("Publishers");

        group.MapGet("/", GetPublishers);
        group.MapGet("/{publisherId:int}", GetPublisherById);
        group.MapPost("/", CreatePublisher).WithValidation<PublisherInput>();
        group.MapGet("/{publisherId:int}/books", GetPublisherBooks);
        group.MapDelete("/{publisherId:int}", DeletePublisher);
    }

    private static async Task<Ok<List<PublisherDto>>> GetPublishers(PubContext db, string? city)
    {
        var query = db.Publishers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(p => p.Address.City == city);
        }

        var publishers = await query
            .OrderBy(p => p.Name)
            .Select(p => new PublisherDto(
                p.PublisherId,
                p.Name,
                p.FoundedYear,
                new AddressDto
                {
                    Street = p.Address.Street,
                    City = p.Address.City,
                    State = p.Address.State,
                    PostalCode = p.Address.PostalCode
                },
                p.Books.Count))
            .ToListAsync();

        return TypedResults.Ok(publishers);
    }

    private static async Task<Results<Ok<PublisherDto>, NotFound>> GetPublisherById(int publisherId, PubContext db)
    {
        var result = await db.Publishers
            .AsNoTracking()
            .Where(p => p.PublisherId == publisherId)
            .Select(p => new { Publisher = p, BookCount = p.Books.Count })
            .SingleOrDefaultAsync();

        return result is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(new PublisherDto(
                result.Publisher.PublisherId,
                result.Publisher.Name,
                result.Publisher.FoundedYear,
                AddressDto.From(result.Publisher.Address),
                result.BookCount));
    }

    private static async Task<Created<PublisherDto>> CreatePublisher(PublisherInput input, PubContext db)
    {
        var publisher = input.ToPublisher();

        await db.Publishers.AddAsync(publisher);
        await db.SaveChangesAsync();

        return TypedResults.Created(
            $"/api/publishers/{publisher.PublisherId}",
            new PublisherDto(publisher.PublisherId, publisher.Name, publisher.FoundedYear, AddressDto.From(publisher.Address), 0));
    }

    private static async Task<Results<Ok<List<BookDto>>, NotFound>> GetPublisherBooks(int publisherId, PubContext db)
    {
        var publisher = await db.Publishers
            .AsNoTracking()
            .Include(p => p.Books.OrderBy(b => b.PublishDate))
            .ThenInclude(b => b.Author)
            .SingleOrDefaultAsync(p => p.PublisherId == publisherId);

        return publisher is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(publisher.Books.Select(BookDto.From).ToList());
    }

    private static async Task<Results<NoContent, NotFound>> DeletePublisher(int publisherId, PubContext db)
    {
        var publisher = await db.Publishers
            .Include(p => p.Books)
            .SingleOrDefaultAsync(p => p.PublisherId == publisherId);

        if (publisher is null)
        {
            return TypedResults.NotFound();
        }

        db.Publishers.Remove(publisher);
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }
}
