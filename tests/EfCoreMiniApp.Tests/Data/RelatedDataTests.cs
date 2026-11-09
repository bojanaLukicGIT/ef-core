using EfCoreMiniApp.Domain;
using EfCoreMiniApp.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EfCoreMiniApp.Tests.Data;

public class RelatedDataTests
{
    [Fact]
    public void Include_And_ThenInclude_LoadMultipleLevels()
    {
        using var database = new SqliteDatabase();
        using var context = database.CreateContext();

        var author = context.Authors
            .AsNoTracking()
            .Include(a => a.Books)
            .ThenInclude(b => b.Cover!)
            .ThenInclude(c => c.Artists)
            .Single(a => a.AuthorId == 4);

        Assert.Equal(2, author.Books.Count);
        var leftHand = author.Books.Single(b => b.Title == "The Left Hand of Darkness");
        Assert.NotNull(leftHand.Cover);
        Assert.Equal(["Bell", "Picasso"], leftHand.Cover!.Artists.Select(a => a.LastName).Order());
    }

    [Fact]
    public void FilteredInclude_LoadsOnlyMatchingBooks()
    {
        using var database = new SqliteDatabase();
        using var context = database.CreateContext();
        var since = new DateOnly(2010, 1, 1);

        var authors = context.Authors
            .Include(a => a.Books.Where(b => b.PublishDate >= since).OrderBy(b => b.Title))
            .ToList();

        Assert.Equal(["Shift", "Wool"], authors.Single(a => a.AuthorId == 5).Books.Select(b => b.Title));
        Assert.Empty(authors.Single(a => a.AuthorId == 1).Books);
    }

    [Fact]
    public void ExplicitLoading_LoadsCollectionAndReferenceOnDemand()
    {
        using var database = new SqliteDatabase();
        using var context = database.CreateContext();
        var author = context.Authors.Single(a => a.AuthorId == 5);

        Assert.Empty(author.Books);
        context.Entry(author).Collection(a => a.Books).Load();
        Assert.True(context.Entry(author).Collection(a => a.Books).IsLoaded);
        Assert.Equal(2, author.Books.Count);

        var shift = author.Books.Single(b => b.Title == "Shift");
        context.Entry(shift).Reference(b => b.Cover).Load();
        Assert.Equal("Rows and rows of freezers", shift.Cover!.DesignIdeas);
    }

    [Fact]
    public void ExplicitLoadingQuery_FiltersRelatedDataInTheDatabase()
    {
        using var database = new SqliteDatabase();
        using var context = database.CreateContext();
        var author = context.Authors.Single(a => a.AuthorId == 5);

        var count = context.Entry(author)
            .Collection(a => a.Books)
            .Query()
            .Count(b => b.PublishDate.Year == 2013);

        Assert.Equal(1, count);
    }

    [Fact]
    public void LazyLoadingProxies_LoadNavigationsWhenAccessed()
    {
        using var database = new SqliteDatabase();
        using var context = database.CreateContext(options => options.UseLazyLoadingProxies());

        var author = context.Authors.Single(a => a.AuthorId == 5);

        Assert.NotEqual(typeof(Author), author.GetType());
        Assert.False(context.Entry(author).Collection(a => a.Books).IsLoaded);
        Assert.Equal(2, author.Books.Count);
        Assert.True(context.Entry(author).Collection(a => a.Books).IsLoaded);
        Assert.Equal("Rows and rows of freezers", author.Books.Single(b => b.Title == "Shift").Cover!.DesignIdeas);
    }

    [Fact]
    public void Projection_LoadsRelatedDataWithoutTracking()
    {
        using var database = new SqliteDatabase();
        using var context = database.CreateContext();

        var authors = context.Authors
            .Where(a => a.AuthorId == 4)
            .Select(a => new { a.LastName, Titles = a.Books.OrderBy(b => b.Title).Select(b => b.Title).ToList() })
            .Single();

        Assert.Equal(["A Wizard of Earthsea", "The Left Hand of Darkness"], authors.Titles);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public void FilteringOnRelatedData_UsesAnyInTheDatabase()
    {
        using var database = new SqliteDatabase();
        using var context = database.CreateContext();

        var recentAuthors = context.Authors
            .Where(a => a.Books.Any(b => b.PublishDate.Year >= 2015))
            .Select(a => a.LastName)
            .ToList();

        Assert.Equal(["Segovia"], recentAuthors);
    }

    [Fact]
    public void ModifyingTrackedRelatedData_IsSavedWithTheParent()
    {
        using var database = new SqliteDatabase();
        using (var context = database.CreateContext())
        {
            var author = context.Authors.Include(a => a.Books).Single(a => a.AuthorId == 4);
            author.Books.Single(b => b.BookId == 9).BasePrice = 10.50m;
            author.LastName = "Le Guin";

            context.ChangeTracker.DetectChanges();
            Assert.Equal(2, context.ChangeTracker.Entries().Count(e => e.State == EntityState.Modified));
            context.SaveChanges();
        }

        using (var context = database.CreateContext())
        {
            Assert.Equal(10.50m, context.Books.Single(b => b.BookId == 9).BasePrice);
            Assert.Equal("Le Guin", context.Authors.Single(a => a.AuthorId == 4).LastName);
        }
    }
}
