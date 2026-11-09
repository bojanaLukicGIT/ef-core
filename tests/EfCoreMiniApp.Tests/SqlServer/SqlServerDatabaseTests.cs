using EfCoreMiniApp.Data;
using EfCoreMiniApp.Data.Interceptors;
using EfCoreMiniApp.Domain;
using EfCoreMiniApp.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EfCoreMiniApp.Tests.SqlServer;

[Collection(SqlServerCollection.Name)]
[Trait("Category", "SqlServer")]
public class SqlServerDatabaseTests(SqlServerDatabaseFixture fixture)
{
    [Fact]
    public void Migrations_AreAllAppliedAndNothingIsPending()
    {
        using var context = fixture.CreateContext();

        var applied = context.Database.GetAppliedMigrations().ToList();

        Assert.Empty(context.Database.GetPendingMigrations());
        Assert.Equal(3, applied.Count);
        Assert.EndsWith("_InitialCreate", applied[0]);
        Assert.EndsWith("_PracticalMappings", applied[1]);
        Assert.EndsWith("_ViewAndStoredProcedures", applied[2]);
    }

    [Fact]
    public void SeedData_IncludesValuesAddedByMigrations()
    {
        using var context = fixture.CreateContext();

        var blueHarbor = context.Publishers.AsNoTracking().Single(p => p.PublisherId == 1);

        Assert.Equal(7, context.Authors.Count());
        Assert.Equal("Portland", blueHarbor.Address.City);
        Assert.Equal(["UKL"], context.Authors.AsNoTracking().Single(a => a.AuthorId == 4).Nicknames);
    }

    [Fact]
    public void FromSql_WithInterpolation_IsParameterized()
    {
        using var context = fixture.CreateContext();
        var pattern = "L%";

        var query = context.Authors.FromSql($"SELECT * FROM Authors WHERE LastName LIKE {pattern}").OrderBy(a => a.LastName);
        var lastNames = query.Select(a => a.LastName).ToList();

        Assert.Contains("LIKE @p0", query.ToQueryString());
        Assert.Equal(["LeGuin", "Lerman"], lastNames);
    }

    [Fact]
    public void FromSqlRaw_WithPositionalParameter_IsParameterized()
    {
        using var context = fixture.CreateContext();

        var query = context.Authors.FromSqlRaw("SELECT * FROM Authors WHERE LastName LIKE {0}", "O%");

        Assert.Contains("LIKE @p0", query.ToQueryString());
        Assert.Equal(["Ozeki"], query.Select(a => a.LastName).ToList());
    }

    [Fact]
    public void FromSqlRaw_WithMaliciousInput_TreatsItAsData()
    {
        using var context = fixture.CreateContext();

        var authors = context.Authors.FromSqlRaw("SELECT * FROM Authors WHERE LastName = {0}", "x' OR '1'='1").ToList();

        Assert.Empty(authors);
    }

    [Fact]
    public async Task StoredProcedure_ReturnsAuthorsPublishedInYearRange()
    {
        await using var context = fixture.CreateContext();
        var start = 2010;
        var end = 2015;

        var authors = await context.Database
            .SqlQuery<AuthorSummary>($"EXEC dbo.AuthorsPublishedinYearRange {start}, {end}")
            .ToListAsync();

        Assert.Equal(["Howey", "Ozeki"], authors.Select(a => a.LastName).Order());
    }

    [Fact]
    public async Task SqlQuery_MapsStoredProcedureResultsToUnmappedType()
    {
        await using var context = fixture.CreateContext();

        var names = await context.Database.SqlQuery<AuthorName>($"EXEC dbo.GetAuthorNames").ToListAsync();

        Assert.Equal(7, names.Count);
        Assert.Contains(names, n => n.FirstName == "Ursula K." && n.LastName == "LeGuin");
    }

    [Fact]
    public async Task SqlQuery_ReturnsScalarValuesAndCanBeComposed()
    {
        await using var context = fixture.CreateContext();
        var minLength = 20;

        var longTitles = await context.Database
            .SqlQuery<string>($"SELECT BookTitle AS Value FROM Books WHERE LEN(BookTitle) > {minLength}")
            .ToListAsync();
        var bookCount = await context.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM Books").SingleAsync();
        var titlesWithThe = await context.Database
            .SqlQuery<string>($"SELECT BookTitle AS Value FROM Books")
            .Where(title => title.Contains("The"))
            .ToListAsync();

        Assert.Equal(await context.Books.CountAsync(b => b.Title.Length > minLength), longTitles.Count);
        Assert.Equal(await context.Books.CountAsync(), bookCount);
        Assert.Equal(await context.Books.CountAsync(b => b.Title.Contains("The")), titlesWithThe.Count);
    }

    [Fact]
    public async Task ExecuteSql_RunsTheDeleteCoverStoredProcedure()
    {
        int coverId;
        await using (var context = fixture.CreateContext())
        {
            var book = new Book { Title = "Cover To Delete", PublishDate = new DateOnly(2020, 5, 1), AuthorId = 7, Genre = BookGenre.History };
            book.Cover = new Cover { DesignIdeas = "Temporary design" };
            context.Books.Add(book);
            await context.SaveChangesAsync();
            coverId = book.Cover.CoverId;
        }

        await using (var context = fixture.CreateContext())
        {
            var affected = await context.Database.ExecuteSqlAsync($"EXEC dbo.DeleteCover {coverId}");

            Assert.Equal(1, affected);
            Assert.False(await context.Covers.AnyAsync(c => c.CoverId == coverId));
        }
    }

    [Fact]
    public async Task KeylessEntity_ReadsFromTheViewWithoutTracking()
    {
        await using var context = fixture.CreateContext();

        var all = await context.AuthorsByArtist.ToListAsync();
        var kArtists = await context.AuthorsByArtist
            .Where(a => a.Artist.StartsWith("K"))
            .OrderBy(a => a.Artist)
            .ThenBy(a => a.Author)
            .ToListAsync();

        Assert.True(all.Count >= 7);
        Assert.Contains(kArtists, row => row.Artist == "Kir Talmage" && row.Author == null);
        Assert.Contains(kArtists, row => row.Artist == "Katharine Kuharic" && row.Author == "Ruth Ozeki");
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task CoverInsertUpdateAndDelete_AreMappedToStoredProcedures()
    {
        var log = new List<string>();
        await using var context = fixture.CreateContext(options =>
            options.LogTo(log.Add, [DbLoggerCategory.Database.Command.Name], LogLevel.Information));

        var book = new Book { Title = "Stored Procedure Cover", PublishDate = new DateOnly(2021, 1, 1), AuthorId = 7, Genre = BookGenre.History };
        context.Books.Add(book);
        await context.SaveChangesAsync();

        book.Cover = new Cover { DesignIdeas = "First idea" };
        await context.SaveChangesAsync();
        Assert.Contains(log, entry => entry.Contains("[CoverInsert]"));
        Assert.NotEqual(0, book.Cover.CoverId);

        book.Cover.DesignIdeas = "Second idea";
        await context.SaveChangesAsync();
        Assert.Contains(log, entry => entry.Contains("[CoverUpdate]"));

        book.Cover = null;
        await context.SaveChangesAsync();
        Assert.Contains(log, entry => entry.Contains("[DeleteCover]"));
        Assert.False(await context.Covers.AnyAsync(c => c.BookId == book.BookId));
    }

    [Fact]
    public async Task ReplacingCover_UsesDeleteAndInsertProcedures()
    {
        await using var context = fixture.CreateContext();
        var book = new Book { Title = "Replace Me", PublishDate = new DateOnly(2021, 2, 1), AuthorId = 7, Genre = BookGenre.History };
        book.Cover = new Cover { DesignIdeas = "Old cover" };
        context.Books.Add(book);
        await context.SaveChangesAsync();
        var oldCoverId = book.Cover.CoverId;

        book.Cover = new Cover { DesignIdeas = "New cover" };
        await context.SaveChangesAsync();

        var covers = await context.Covers.AsNoTracking().Where(c => c.BookId == book.BookId).ToListAsync();
        Assert.Single(covers);
        Assert.NotEqual(oldCoverId, covers[0].CoverId);
        Assert.Equal("New cover", covers[0].DesignIdeas);
    }

    [Fact]
    public async Task RowVersion_DetectsConcurrentUpdates()
    {
        int bookId;
        await using (var setup = fixture.CreateContext())
        {
            var book = new Book { Title = "Concurrency", PublishDate = new DateOnly(2022, 1, 1), AuthorId = 7, Genre = BookGenre.History, BasePrice = 10m };
            setup.Books.Add(book);
            await setup.SaveChangesAsync();
            bookId = book.BookId;
            Assert.NotNull(book.RowVersion);
        }

        await using var firstUser = fixture.CreateContext();
        await using var secondUser = fixture.CreateContext();
        var firstCopy = await firstUser.Books.SingleAsync(b => b.BookId == bookId);
        var secondCopy = await secondUser.Books.SingleAsync(b => b.BookId == bookId);
        var originalVersion = firstCopy.RowVersion!;

        firstCopy.BasePrice = 11m;
        await firstUser.SaveChangesAsync();
        Assert.NotEqual(originalVersion, firstCopy.RowVersion);

        secondCopy.BasePrice = 12m;
        var exception = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondUser.SaveChangesAsync());

        var entry = exception.Entries.Single();
        var databaseValues = await entry.GetDatabaseValuesAsync();
        Assert.Equal(11m, databaseValues!.GetValue<decimal>(nameof(Book.BasePrice)));

        entry.OriginalValues.SetValues(databaseValues);
        await secondUser.SaveChangesAsync();

        await using var verify = fixture.CreateContext();
        Assert.Equal(12m, (await verify.Books.SingleAsync(b => b.BookId == bookId)).BasePrice);
    }

    [Fact]
    public async Task CommandInterceptor_AddsQueryHintOnlyToTaggedQueries()
    {
        var log = new List<string>();
        await using var context = fixture.CreateContext(options =>
            options.LogTo(log.Add, [DbLoggerCategory.Database.Command.Name], LogLevel.Information));

        var tagged = await context.Authors.TagWith(QueryHintCommandInterceptor.RobustPlanTag).ToListAsync();
        var untagged = await context.Books.ToListAsync();

        Assert.NotEmpty(tagged);
        Assert.NotEmpty(untagged);
        Assert.Single(log, entry => entry.Contains("OPTION (ROBUST PLAN)"));
        Assert.Contains(log, entry => entry.Contains("FROM [Authors]") && entry.Contains("OPTION (ROBUST PLAN)"));
    }

    [Fact]
    public async Task Transaction_RollbackRestoresDataOnSqlServer()
    {
        await using (var context = fixture.CreateContext())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            context.Artists.Add(new Artist { FirstName = "Rolled", LastName = "Back" });
            await context.SaveChangesAsync();
            await context.Publishers.Where(p => p.PublisherId == 3).ExecuteUpdateAsync(setters => setters.SetProperty(p => p.Name, "Changed"));
            await transaction.RollbackAsync();
        }

        await using (var context = fixture.CreateContext())
        {
            Assert.False(await context.Artists.AnyAsync(a => a.LastName == "Back"));
            Assert.Equal("Lantern House", (await context.Publishers.SingleAsync(p => p.PublisherId == 3)).Name);
        }
    }
}
