using EfCoreMiniApp.Data;
using EfCoreMiniApp.Domain;
using EfCoreMiniApp.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EfCoreMiniApp.Tests.Data;

public class ManyToManyTests
{
    [Fact]
    public void ConnectingExistingArtistAndCover_CreatesJoinRowWithDatabaseGeneratedPayload()
    {
        using var database = new SqliteDatabase();
        using (var context = database.CreateContext())
        {
            var artist = context.Artists.Find(4)!;
            var cover = context.Covers.Find(2)!;

            cover.Artists.Add(artist);
            context.SaveChanges();
        }

        using (var context = database.CreateContext())
        {
            var assignment = context.Set<CoverAssignment>().Single(ca => ca.ArtistId == 4 && ca.CoverId == 2);
            Assert.NotEqual(default, assignment.DateCreated);
            Assert.True(assignment.DateCreated > SeedData.SeedDate);
        }
    }

    [Fact]
    public void NewCoverWithExistingArtist_InsertsCoverAndJoinRow()
    {
        using var database = new SqliteDatabase();
        using (var context = database.CreateContext())
        {
            var artist = context.Artists.Find(1)!;
            var cover = new Cover { BookId = 4, DesignIdeas = "Author has provided a photo" };
            cover.Artists.Add(artist);

            context.Covers.Add(cover);
            context.SaveChanges();
        }

        using (var context = database.CreateContext())
        {
            var cover = context.Covers.Include(c => c.Artists).Single(c => c.BookId == 4);
            Assert.Equal("Picasso", cover.Artists.Single().LastName);
        }
    }

    [Fact]
    public void NewArtistAndNewCover_AreInsertedTogether()
    {
        using var database = new SqliteDatabase();
        using (var context = database.CreateContext())
        {
            var artist = new Artist { FirstName = "Kir", LastName = "Talmage Jr." };
            artist.Covers.Add(new Cover { BookId = 6, DesignIdeas = "We like birds!" });

            context.Artists.Add(artist);
            context.SaveChanges();
        }

        using (var context = database.CreateContext())
        {
            Assert.True(context.Artists.Any(a => a.LastName == "Talmage Jr." && a.Covers.Any(c => c.BookId == 6)));
        }
    }

    [Fact]
    public void QueryingAcrossSkipNavigations_WorksInBothDirections()
    {
        using var database = new SqliteDatabase();
        using var context = database.CreateContext();

        var artistsWithCovers = context.Artists.Where(a => a.Covers.Any()).Select(a => a.LastName).OrderBy(n => n).ToList();
        var picassoCovers = context.Artists.Include(a => a.Covers).Single(a => a.ArtistId == 1).Covers.Select(c => c.CoverId).Order();
        var coverOneArtists = context.Covers.Include(c => c.Artists).Single(c => c.CoverId == 1).Artists.Select(a => a.LastName).Order();
        var booksIllustratedByBell = context.Books.Where(b => b.Cover!.Artists.Any(a => a.LastName == "Bell")).Select(b => b.Title).OrderBy(t => t).ToList();

        Assert.Equal(["Bell", "Kuharic", "Picasso"], artistsWithCovers);
        Assert.Equal([1, 3], picassoCovers);
        Assert.Equal(["Bell", "Picasso"], coverOneArtists);
        Assert.Equal(["Shift", "The Left Hand of Darkness"], booksIllustratedByBell);
    }

    [Fact]
    public void RemovingArtistFromCover_DeletesOnlyTheJoinRow()
    {
        using var database = new SqliteDatabase();
        using (var context = database.CreateContext())
        {
            var cover = context.Covers
                .Include(c => c.Artists.Where(a => a.ArtistId == 1))
                .Single(c => c.CoverId == 1);

            cover.Artists.RemoveAt(0);
            context.ChangeTracker.DetectChanges();

            Assert.Contains(context.ChangeTracker.Entries<CoverAssignment>(), e => e.State == EntityState.Deleted);
            context.SaveChanges();
        }

        using (var context = database.CreateContext())
        {
            Assert.False(context.Set<CoverAssignment>().Any(ca => ca.ArtistId == 1 && ca.CoverId == 1));
            Assert.True(context.Artists.Any(a => a.ArtistId == 1));
            Assert.True(context.Covers.Any(c => c.CoverId == 1));
            Assert.Equal(["Bell"], context.Covers.Where(c => c.CoverId == 1).SelectMany(c => c.Artists).Select(a => a.LastName));
        }
    }

    [Fact]
    public void ReassigningCover_ReplacesTheJoinRow()
    {
        using var database = new SqliteDatabase();
        using (var context = database.CreateContext())
        {
            var cover = context.Covers
                .Include(c => c.Artists.Where(a => a.ArtistId == 3))
                .Single(c => c.CoverId == 5);

            cover.Artists.RemoveAt(0);
            cover.Artists.Add(context.Artists.Find(4)!);
            context.SaveChanges();
        }

        using (var context = database.CreateContext())
        {
            var artistIds = context.Set<CoverAssignment>().Where(ca => ca.CoverId == 5).Select(ca => ca.ArtistId).ToList();
            Assert.Equal([4], artistIds);
        }
    }

    [Fact]
    public void ExplicitJoinEntity_ExposesThePayload()
    {
        using var database = new SqliteDatabase();
        using var context = database.CreateContext();

        var assignments = context.Set<CoverAssignment>().AsNoTracking().ToList();

        Assert.Equal(6, assignments.Count);
        Assert.All(assignments, assignment => Assert.Equal(SeedData.SeedDate, assignment.DateCreated));
    }

    [Fact]
    public void DeletingArtist_CascadesToJoinRowsButKeepsCovers()
    {
        using var database = new SqliteDatabase();
        using (var context = database.CreateContext())
        {
            context.Artists.Remove(context.Artists.Find(2)!);
            context.SaveChanges();
        }

        using (var context = database.CreateContext())
        {
            Assert.False(context.Set<CoverAssignment>().Any(ca => ca.ArtistId == 2));
            Assert.Equal(5, context.Covers.Count());
        }
    }
}
