using EfCoreMiniApp.Domain;
using EfCoreMiniApp.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EfCoreMiniApp.Tests.Data;

public class CrudTests
{
    [Fact]
    public void Add_InsertsAuthorAndAssignsGeneratedKey()
    {
        using var database = new SqliteDatabase();
        using var context = database.CreateContext();
        var author = new Author { FirstName = "Madeline", LastName = "Miller" };

        context.Authors.Add(author);
        context.SaveChanges();

        Assert.NotEqual(0, author.AuthorId);
    }

    [Fact]
    public async Task AddAsync_InsertsPublisherThatCanBeReadInNewContext()
    {
        using var database = new SqliteDatabase();
        int publisherId;

        await using (var context = database.CreateContext())
        {
            var publisher = new Publisher { Name = "Test Press", FoundedYear = 2020 };
            await context.Publishers.AddAsync(publisher);
            await context.SaveChangesAsync();
            publisherId = publisher.PublisherId;
        }

        await using (var context = database.CreateContext())
        {
            var publisher = await context.Publishers.SingleAsync(p => p.PublisherId == publisherId);
            Assert.Equal("Test Press", publisher.Name);
            Assert.Equal(2020, publisher.FoundedYear);
        }
    }

    [Fact]
    public async Task AddRange_InsertsMultipleAuthorsInOneSaveChanges()
    {
        using var database = new SqliteDatabase();
        await using var context = database.CreateContext();

        context.Authors.AddRange(
            new Author { FirstName = "Don", LastName = "Jones" },
            new Author { FirstName = "Jim", LastName = "Christopher" },
            new Author { FirstName = "Stephen", LastName = "Haunts" });

        var saved = await context.SaveChangesAsync();

        Assert.Equal(3, saved);
        Assert.Equal(10, await context.Authors.CountAsync());
    }

    [Fact]
    public async Task SeedData_IsCreatedWithTheDatabase()
    {
        using var database = new SqliteDatabase();
        await using var context = database.CreateContext();

        Assert.Equal(7, await context.Authors.CountAsync());
        Assert.Equal(9, await context.Books.CountAsync());
        Assert.Equal(3, await context.Publishers.CountAsync());
        Assert.Equal(4, await context.Artists.CountAsync());
        Assert.Equal(5, await context.Covers.CountAsync());
    }

    [Fact]
    public async Task Update_ChangesTrackedAuthorInDatabase()
    {
        using var database = new SqliteDatabase();

        await using (var context = database.CreateContext())
        {
            var author = await context.Authors.SingleAsync(a => a.AuthorId == 2);
            author.FirstName = "Ruth L.";
            await context.SaveChangesAsync();
        }

        await using (var context = database.CreateContext())
        {
            var author = await context.Authors.SingleAsync(a => a.AuthorId == 2);
            Assert.Equal("Ruth L.", author.FirstName);
        }
    }

    [Fact]
    public async Task Remove_DeletesAuthorWithoutBooks()
    {
        using var database = new SqliteDatabase();

        await using (var context = database.CreateContext())
        {
            var author = await context.Authors.FindAsync(7);
            context.Authors.Remove(author!);
            await context.SaveChangesAsync();
        }

        await using (var context = database.CreateContext())
        {
            Assert.False(await context.Authors.AnyAsync(a => a.AuthorId == 7));
        }
    }
}
