using EfCoreMiniApp.Api.Dtos;
using EfCoreMiniApp.Api.Validation;
using EfCoreMiniApp.Data;
using EfCoreMiniApp.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EfCoreMiniApp.Api.Endpoints;

public static class CoverEndpoints
{
    public static void MapCoverEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/covers").WithTags("Covers");

        group.MapGet("/", GetCovers);
        group.MapGet("/{coverId:int}", GetCoverById);
        group.MapPost("/{coverId:int}/artists/{artistId:int}", AssignArtist);
        group.MapDelete("/{coverId:int}/artists/{artistId:int}", UnassignArtist);
        group.MapPut("/{coverId:int}/artists/{artistId:int}", ReassignArtist).WithValidation<ReassignArtistInput>();
    }

    private static async Task<Ok<List<CoverDetailsDto>>> GetCovers(PubContext db)
    {
        var covers = await db.Covers
            .AsNoTracking()
            .OrderBy(c => c.CoverId)
            .Select(c => new
            {
                c.CoverId,
                c.DesignIdeas,
                c.DigitalOnly,
                c.BookId,
                BookTitle = c.Book.Title,
                Artists = c.Artists.OrderBy(a => a.LastName).Select(a => new { a.ArtistId, a.FirstName, a.LastName })
            })
            .ToListAsync();

        var assignments = await db.Set<CoverAssignment>().AsNoTracking().ToListAsync();

        return TypedResults.Ok(covers
            .Select(c => new CoverDetailsDto(
                c.CoverId,
                c.DesignIdeas,
                c.DigitalOnly,
                c.BookId,
                c.BookTitle,
                c.Artists
                    .Select(a => new CoverArtistDto(
                        a.ArtistId,
                        $"{a.FirstName} {a.LastName}",
                        assignments.Single(ca => ca.CoverId == c.CoverId && ca.ArtistId == a.ArtistId).DateCreated))
                    .ToList()))
            .ToList());
    }

    private static async Task<Results<Ok<CoverDetailsDto>, NotFound>> GetCoverById(int coverId, PubContext db)
    {
        var cover = await db.Covers
            .AsNoTracking()
            .Where(c => c.CoverId == coverId)
            .Select(c => new { c.CoverId, c.DesignIdeas, c.DigitalOnly, c.BookId, BookTitle = c.Book.Title })
            .SingleOrDefaultAsync();

        if (cover is null)
        {
            return TypedResults.NotFound();
        }

        var artists = await (
                from assignment in db.Set<CoverAssignment>()
                join artist in db.Artists on assignment.ArtistId equals artist.ArtistId
                where assignment.CoverId == coverId
                orderby artist.LastName
                select new CoverArtistDto(artist.ArtistId, artist.FirstName + " " + artist.LastName, assignment.DateCreated))
            .ToListAsync();

        return TypedResults.Ok(new CoverDetailsDto(
            cover.CoverId,
            cover.DesignIdeas,
            cover.DigitalOnly,
            cover.BookId,
            cover.BookTitle,
            artists));
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> AssignArtist(
        int coverId,
        int artistId,
        PubContext db)
    {
        var cover = await db.Covers
            .Include(c => c.Artists.Where(a => a.ArtistId == artistId))
            .SingleOrDefaultAsync(c => c.CoverId == coverId);

        if (cover is null)
        {
            return NotFound($"Cover {coverId} does not exist.");
        }

        if (cover.Artists.Count > 0)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title = "Artist already assigned",
                Detail = $"Artist {artistId} is already assigned to cover {coverId}.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var artist = await db.Artists.FindAsync(artistId);
        if (artist is null)
        {
            return NotFound($"Artist {artistId} does not exist.");
        }

        cover.Artists.Add(artist);
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>>> UnassignArtist(int coverId, int artistId, PubContext db)
    {
        var cover = await db.Covers
            .Include(c => c.Artists.Where(a => a.ArtistId == artistId))
            .SingleOrDefaultAsync(c => c.CoverId == coverId);

        if (cover is null || cover.Artists.Count == 0)
        {
            return NotFound($"Artist {artistId} is not assigned to cover {coverId}.");
        }

        cover.Artists.RemoveAt(0);
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> ReassignArtist(
        int coverId,
        int artistId,
        ReassignArtistInput input,
        PubContext db)
    {
        var cover = await db.Covers
            .Include(c => c.Artists.Where(a => a.ArtistId == artistId || a.ArtistId == input.NewArtistId))
            .SingleOrDefaultAsync(c => c.CoverId == coverId);

        var currentArtist = cover?.Artists.SingleOrDefault(a => a.ArtistId == artistId);
        if (cover is null || currentArtist is null)
        {
            return NotFound($"Artist {artistId} is not assigned to cover {coverId}.");
        }

        if (cover.Artists.Any(a => a.ArtistId == input.NewArtistId))
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title = "Artist already assigned",
                Detail = $"Artist {input.NewArtistId} is already assigned to cover {coverId}.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var newArtist = await db.Artists.FindAsync(input.NewArtistId);
        if (newArtist is null)
        {
            return NotFound($"Artist {input.NewArtistId} does not exist.");
        }

        cover.Artists.Remove(currentArtist);
        cover.Artists.Add(newArtist);
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    private static NotFound<ProblemDetails> NotFound(string detail) =>
        TypedResults.NotFound(new ProblemDetails
        {
            Title = "Not found",
            Detail = detail,
            Status = StatusCodes.Status404NotFound
        });
}
