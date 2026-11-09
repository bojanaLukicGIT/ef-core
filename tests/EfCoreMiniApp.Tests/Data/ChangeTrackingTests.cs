using EfCoreMiniApp.Domain;
using EfCoreMiniApp.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EfCoreMiniApp.Tests.Data;

public class ChangeTrackingTests
{
    [Fact]
    public void Add_MarksEntityAsAdded()
    {
        using var database = new SqliteDatabase();
        using var context = database.CreateContext();
        var author = new Author { FirstName = "a", LastName = "b" };

        context.Authors.Add(author);

        Assert.Equal(EntityState.Added, context.Entry(author).State);
    }

    [Fact]
    public void ChangingTrackedEntity_IsDetectedAsModified()
    {
        using var database = new SqliteDatabase();
        using var context = database.CreateContext();
        var author = context.Authors.Single(a => a.AuthorId == 1);

        author.FirstName = "Rhoda B.";
        context.ChangeTracker.DetectChanges();

        var entry = context.Entry(author);
        Assert.Equal(EntityState.Modified, entry.State);
        Assert.True(entry.Property(a => a.FirstName).IsModified);
        Assert.False(entry.Property(a => a.LastName).IsModified);
        Assert.Contains("Modified", context.ChangeTracker.DebugView.ShortView);
    }

    [Fact]
    public void Remove_MarksEntityAsDeleted()
    {
        using var database = new SqliteDatabase();
        using var context = database.CreateContext();
        var author = context.Authors.Single(a => a.AuthorId == 7);

        context.Authors.Remove(author);

        Assert.Equal(EntityState.Deleted, context.Entry(author).State);
    }

    [Fact]
    public void AsNoTracking_ReturnsDetachedEntities()
    {
        using var database = new SqliteDatabase();
        using var context = database.CreateContext();

        var authors = context.Authors.AsNoTracking().ToList();

        Assert.NotEmpty(authors);
        Assert.Empty(context.ChangeTracker.Entries());
        Assert.Equal(EntityState.Detached, context.Entry(authors[0]).State);
    }

    [Fact]
    public void ChangeTrackerEntries_ReportEveryTrackedEntity()
    {
        using var database = new SqliteDatabase();
        using var context = database.CreateContext();

        var lermanBooks = context.Books.Where(b => b.AuthorId == 1).ToList();
        lermanBooks[0].BasePrice += 1;
        context.Authors.Add(new Author { FirstName = "Frank", LastName = "Herbert Jr." });

        var states = context.ChangeTracker.Entries()
            .GroupBy(e => e.State)
            .ToDictionary(g => g.Key, g => g.Count());

        Assert.Equal(1, states[EntityState.Modified]);
        Assert.Equal(1, states[EntityState.Unchanged]);
        Assert.Equal(1, states[EntityState.Added]);
    }

    [Fact]
    public void EntryState_MarksOnlyTheRoot_WhileUpdate_MarksTheWholeGraph()
    {
        using var database = new SqliteDatabase();
        Book detachedBook;
        using (var context = database.CreateContext())
        {
            detachedBook = context.Books.AsNoTracking().Include(b => b.Author).Single(b => b.BookId == 4);
        }

        detachedBook.BasePrice = 15m;
        detachedBook.Author.LastName = "Howey-Updated";

        using (var context = database.CreateContext())
        {
            context.Entry(detachedBook).State = EntityState.Modified;

            Assert.Equal(EntityState.Modified, context.Entry(detachedBook).State);
            Assert.Equal(EntityState.Detached, context.Entry(detachedBook.Author).State);
            context.SaveChanges();
        }

        using (var context = database.CreateContext())
        {
            Assert.Equal(15m, context.Books.Single(b => b.BookId == 4).BasePrice);
            Assert.Equal("Howey", context.Authors.Single(a => a.AuthorId == 5).LastName);
        }

        using (var context = database.CreateContext())
        {
            context.Books.Update(detachedBook);

            Assert.Equal(EntityState.Modified, context.Entry(detachedBook).State);
            Assert.Equal(EntityState.Modified, context.Entry(detachedBook.Author).State);
            context.SaveChanges();
        }

        using (var context = database.CreateContext())
        {
            Assert.Equal("Howey-Updated", context.Authors.Single(a => a.AuthorId == 5).LastName);
        }
    }

    [Fact]
    public void Update_OnDetachedGraph_AddsChildrenWithoutKeys()
    {
        using var database = new SqliteDatabase();
        Author detachedAuthor;
        using (var context = database.CreateContext())
        {
            detachedAuthor = context.Authors.AsNoTracking().Single(a => a.AuthorId == 7);
        }

        var newBook = new Book { Title = "Dune", PublishDate = new DateOnly(1965, 8, 1), Genre = BookGenre.ScienceFiction };
        detachedAuthor.Books.Add(newBook);

        using (var context = database.CreateContext())
        {
            context.Authors.Update(detachedAuthor);

            Assert.Equal(EntityState.Modified, context.Entry(detachedAuthor).State);
            Assert.Equal(EntityState.Added, context.Entry(newBook).State);
            context.SaveChanges();
        }

        using (var context = database.CreateContext())
        {
            Assert.True(context.Books.Any(b => b.Title == "Dune" && b.AuthorId == 7));
        }
    }

    [Fact]
    public void ModifyingDetachedRelatedEntity_WithEntryState_UpdatesOnlyThatEntity()
    {
        using var database = new SqliteDatabase();
        Author author;
        using (var context = database.CreateContext())
        {
            author = context.Authors.AsNoTracking().Include(a => a.Books.OrderBy(b => b.BookId)).Single(a => a.AuthorId == 5);
        }

        author.Books[0].BasePrice = 12m;
        author.Books[1].BasePrice = 99m;

        using (var context = database.CreateContext())
        {
            context.Entry(author.Books[0]).State = EntityState.Modified;
            var saved = context.SaveChanges();
            Assert.Equal(1, saved);
        }

        using (var context = database.CreateContext())
        {
            var prices = context.Books.Where(b => b.AuthorId == 5).OrderBy(b => b.BookId).Select(b => b.BasePrice).ToList();
            Assert.Equal([12m, 14m], prices);
        }
    }
}
