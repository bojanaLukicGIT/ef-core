using EfCoreMiniApp.Api.Dtos;
using EfCoreMiniApp.Api.Validation;
using EfCoreMiniApp.Data;
using EfCoreMiniApp.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace EfCoreMiniApp.Api.Endpoints;

public static class ArtistEndpoints
{
    public static void MapArtistEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/artists").WithTags("Artists");

        group.MapGet("/", GetArtists);
        group.MapGet("/{artistId:int}", GetArtistById);
        group.MapPost("/", CreateArtist).WithValidation<ArtistInput>();
    }

    private static async Task<Ok<List<ArtistDto>>> GetArtists(PubContext db, bool withCoversOnly = false)
    {
        var query = db.Artists.AsNoTracking();
        if (withCoversOnly)
        {
            query = query.Where(a => a.Covers.Any());
        }

        var artists = await query
            .OrderBy(a => a.LastName)
            .ThenBy(a => a.FirstName)
            .Select(a => new ArtistDto(a.ArtistId, a.FirstName, a.LastName, a.Notes, a.Covers.Count))
            .ToListAsync();

        return TypedResults.Ok(artists);
    }

    private static async Task<Results<Ok<ArtistDetailsDto>, NotFound>> GetArtistById(int artistId, PubContext db)
    {
        var artist = await db.Artists
            .AsNoTracking()
            .Include(a => a.Covers)
            .ThenInclude(c => c.Book)
            .SingleOrDefaultAsync(a => a.ArtistId == artistId);

        if (artist is null)
        {
            return TypedResults.NotFound();
        }

        var covers = artist.Covers
            .OrderBy(c => c.CoverId)
            .Select(c => new ArtistCoverDto(c.CoverId, c.DesignIdeas, c.BookId, c.Book.Title))
            .ToList();

        return TypedResults.Ok(new ArtistDetailsDto(artist.ArtistId, artist.FirstName, artist.LastName, artist.Notes, covers));
    }

    private static async Task<Created<ArtistDto>> CreateArtist(ArtistInput input, PubContext db)
    {
        var artist = new Artist { FirstName = input.FirstName, LastName = input.LastName, Notes = input.Notes };

        db.Artists.Add(artist);
        await db.SaveChangesAsync();

        return TypedResults.Created(
            $"/api/artists/{artist.ArtistId}",
            new ArtistDto(artist.ArtistId, artist.FirstName, artist.LastName, artist.Notes, 0));
    }
}
