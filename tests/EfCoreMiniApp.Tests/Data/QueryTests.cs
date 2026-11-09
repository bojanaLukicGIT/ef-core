using EfCoreMiniApp.Domain;
using EfCoreMiniApp.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EfCoreMiniApp.Tests.Data;

public class QueryTests
{
    [Fact]
    public async Task Where_OrderBy_ThenBy_FilterAndSortAuthors()
    {
        using var database = new SqliteDatabase();
        await using var context = database.CreateContext();

        var lastNames = await context.Authors
            .Where(a => EF.Functions.Like(a.LastName, "L%"))
            .OrderBy(a => a.LastName)
            .ThenBy(a => a.FirstName)
            .Select(a => a.LastName)
            .ToListAsync();

        Assert.Equal(["LeGuin", "Lerman"], lastNames);
    }

    [Fact]
    public async Task OrderByDescending_ThenByDescending_SortsBooks()
    {
        using var database = new SqliteDatabase();
        await using var context = database.CreateContext();

        var titles = await context.Books
            .Where(b => b.AuthorId == 5)
            .OrderByDescending(b => b.PublishDate)
            .ThenByDescending(b => b.Title)
            .Select(b => b.Title)
            .ToListAsync();

        Assert.Equal(["Shift", "Wool"], titles);
    }

    [Fact]
    public async Task FirstOrDefaultAsync_ReturnsNullWhenNothingMatches()
    {
        using var database = new SqliteDatabase();
        await using var context = database.CreateContext();

        var found = await context.Authors.FirstOrDefaultAsync(a => a.LastName == "Lerman");
        var missing = await context.Authors.FirstOrDefaultAsync(a => a.LastName == "Nobody");

        Assert.NotNull(found);
        Assert.Null(missing);
    }

    [Fact]
    public async Task SingleOrDefaultAsync_ThrowsWhenMoreThanOneRowMatches()
    {
        using var database = new SqliteDatabase();
        await using var context = database.CreateContext();

        var single = await context.Books.SingleOrDefaultAsync(b => b.Title == "Wool");

        Assert.NotNull(single);
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.Books.SingleOrDefaultAsync(b => b.AuthorId == 5));
    }

    [Fact]
    public async Task AnyAsync_And_CountAsync_ReturnScalarResults()
    {
        using var database = new SqliteDatabase();
        await using var context = database.CreateContext();

        Assert.True(await context.Authors.AnyAsync(a => a.LastName == "Howey"));
        Assert.False(await context.Authors.AnyAsync(a => a.LastName == "Tolkien"));
        Assert.Equal(3, await context.Books.CountAsync(b => b.Genre == BookGenre.ScienceFiction));
    }

    [Fact]
    public async Task GroupBy_CountsBooksPerGenre()
    {
        using var database = new SqliteDatabase();
        await using var context = database.CreateContext();

        var counts = await context.Books
            .GroupBy(b => b.Genre)
            .Select(g => new { Genre = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Genre, x => x.Count);

        Assert.Equal(3, counts[BookGenre.HistoricalFiction]);
        Assert.Equal(3, counts[BookGenre.ScienceFiction]);
        Assert.Equal(1, counts[BookGenre.YoungAdult]);
    }

    [Fact]
    public async Task Select_ProjectsIntoUntrackedTypes()
    {
        using var database = new SqliteDatabase();
        await using var context = database.CreateContext();

        var projection = await context.Authors
            .Select(a => new { a.LastName, BookCount = a.Books.Count })
            .OrderBy(a => a.LastName)
            .ToListAsync();

        Assert.Equal(7, projection.Count);
        Assert.Equal(2, projection.Single(a => a.LastName == "Howey").BookCount);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Skip_And_Take_ReturnOnePage()
    {
        using var database = new SqliteDatabase();
        await using var context = database.CreateContext();

        var ids = await context.Authors
            .OrderBy(a => a.AuthorId)
            .Skip(3)
            .Take(3)
            .Select(a => a.AuthorId)
            .ToListAsync();

        Assert.Equal([4, 5, 6], ids);
    }

    [Fact]
    public async Task FindAsync_ReturnsTrackedInstanceWithoutQueryingAgain()
    {
        using var database = new SqliteDatabase();
        await using var context = database.CreateContext();

        var queried = await context.Authors.SingleAsync(a => a.AuthorId == 3);
        var found = await context.Authors.FindAsync(3);

        Assert.Same(queried, found);
    }
}
