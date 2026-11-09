using EfCoreMiniApp.Domain;
using EfCoreMiniApp.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EfCoreMiniApp.Tests.Data;

public class OneToOneTests
{
    [Fact]
    public void Include_LoadsTheDependentCover()
    {
        using var database = new SqliteDatabase();
        using var context = database.CreateContext();

        var books = context.Books.Include(b => b.Cover).ToList();

        Assert.Equal(5, books.Count(b => b.Cover is not null));
        Assert.Equal([4, 6, 7, 9], books.Where(b => b.Cover is null).Select(b => b.BookId).Order());
        Assert.Equal("Should we put a clock?", books.Single(b => b.BookId == 2).Cover!.DesignIdeas);
    }

    [Fact]
    public void Projection_ReadsAcrossTheOneToOneRelationship()
    {
        using var database = new SqliteDatabase();
        using var context = database.CreateContext();

        var digitalTitles = context.Books
            .Where(b => b.Cover != null && b.Cover.DigitalOnly)
            .Select(b => new { b.Title, b.Cover!.DesignIdeas })
            .OrderBy(b => b.Title)
            .ToList();

        Assert.Equal(["A Tale For the Time Being", "Call Me Ishtar"], digitalTitles.Select(b => b.Title));
    }

    [Fact]
    public void AssigningCoverToBookWithoutCover_InsertsDependentWithForeignKey()
    {
        using var database = new SqliteDatabase();
        using (var context = database.CreateContext())
        {
            var wool = context.Books.Find(4)!;
            wool.Cover = new Cover { DesignIdeas = "Rows and rows of freezers" };
            context.SaveChanges();
        }

        using (var context = database.CreateContext())
        {
            Assert.Equal(4, context.Covers.Single(c => c.DesignIdeas == "Rows and rows of freezers" && c.BookId == 4).BookId);
        }
    }

    [Fact]
    public void ReplacingTrackedCover_DeletesTheOldOneAndInsertsTheNewOne()
    {
        using var database = new SqliteDatabase();
        using (var context = database.CreateContext())
        {
            var book = context.Books.Include(b => b.Cover).Single(b => b.BookId == 2);
            var oldCover = book.Cover!;

            book.Cover = new Cover { DesignIdeas = "A spiral" };
            context.ChangeTracker.DetectChanges();

            Assert.Equal(EntityState.Deleted, context.Entry(oldCover).State);
            Assert.Equal(EntityState.Added, context.Entry(book.Cover).State);
            context.SaveChanges();
        }

        using (var context = database.CreateContext())
        {
            var covers = context.Covers.Where(c => c.BookId == 2).ToList();
            Assert.Single(covers);
            Assert.Equal("A spiral", covers[0].DesignIdeas);
            Assert.False(context.Covers.Any(c => c.CoverId == 2));
        }
    }

    [Fact]
    public void RemovingRequiredDependent_DeletesTheCover()
    {
        using var database = new SqliteDatabase();
        using (var context = database.CreateContext())
        {
            var book = context.Books.Include(b => b.Cover).Single(b => b.BookId == 1);
            var cover = book.Cover!;

            book.Cover = null;
            context.ChangeTracker.DetectChanges();

            Assert.Equal(EntityState.Deleted, context.Entry(cover).State);
            context.SaveChanges();
        }

        using (var context = database.CreateContext())
        {
            Assert.True(context.Books.Any(b => b.BookId == 1));
            Assert.False(context.Covers.Any(c => c.BookId == 1));
        }
    }

    [Fact]
    public void DeletingPrincipalBook_CascadesToTheCover()
    {
        using var database = new SqliteDatabase();
        using (var context = database.CreateContext())
        {
            context.Books.Where(b => b.BookId == 3).ExecuteDelete();
        }

        using (var context = database.CreateContext())
        {
            Assert.False(context.Covers.Any(c => c.CoverId == 1));
        }
    }

    [Fact]
    public void UniqueForeignKey_PreventsSecondCoverForTheSameBook()
    {
        using var database = new SqliteDatabase();
        using var context = database.CreateContext();

        context.Covers.Add(new Cover { BookId = 3, DesignIdeas = "A second cover" });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }

    [Fact]
    public void UpdatingExistingCoverInsteadOfReplacing_AvoidsUniqueKeySideEffects()
    {
        using var database = new SqliteDatabase();
        using (var context = database.CreateContext())
        {
            var book = context.Books.Include(b => b.Cover).Single(b => b.BookId == 5);
            if (book.Cover is not null)
            {
                book.Cover.DesignIdeas = "A spirally spiral";
            }
            else
            {
                book.Cover = new Cover { DesignIdeas = "A spirally spiral" };
            }

            context.SaveChanges();
        }

        using (var context = database.CreateContext())
        {
            var cover = context.Covers.Single(c => c.BookId == 5);
            Assert.Equal(4, cover.CoverId);
            Assert.Equal("A spirally spiral", cover.DesignIdeas);
        }
    }
}
