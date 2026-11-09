using EfCoreMiniApp.Domain;
using EfCoreMiniApp.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EfCoreMiniApp.Tests.Data;

public class OneToManyTests
{
    [Fact]
    public void AddingAuthorWithBooks_InsertsTheWholeGraph()
    {
        using var database = new SqliteDatabase();
        using (var context = database.CreateContext())
        {
            var author = new Author { FirstName = "Don", LastName = "Jones" };
            author.Books.AddRange(
            [
                new Book { Title = "The Never", PublishDate = new DateOnly(2019, 12, 1), Genre = BookGenre.Mystery },
                new Book { Title = "Alabaster", PublishDate = new DateOnly(2019, 4, 1), Genre = BookGenre.Mystery }
            ]);

            context.Authors.Add(author);
            Assert.All(author.Books, book => Assert.Equal(EntityState.Added, context.Entry(book).State));
            context.SaveChanges();
        }

        using (var context = database.CreateContext())
        {
            var author = context.Authors.Include(a => a.Books).Single(a => a.LastName == "Jones");
            Assert.Equal(2, author.Books.Count);
            Assert.All(author.Books, book => Assert.Equal(author.AuthorId, book.AuthorId));
        }
    }

    [Fact]
    public void AddingBookThroughTrackedAuthorNavigation_SetsForeignKey()
    {
        using var database = new SqliteDatabase();
        using (var context = database.CreateContext())
        {
            var author = context.Authors.Single(a => a.LastName == "Howey");
            author.Books.Add(new Book { Title = "Dust", PublishDate = new DateOnly(2013, 8, 17), Genre = BookGenre.ScienceFiction });
            context.SaveChanges();
        }

        using (var context = database.CreateContext())
        {
            Assert.Equal(5, context.Books.Single(b => b.Title == "Dust").AuthorId);
        }
    }

    [Fact]
    public void AddingBookWithForeignKeyOnly_DoesNotNeedTheAuthorObject()
    {
        using var database = new SqliteDatabase();
        using var context = database.CreateContext();

        context.Books.Add(new Book { Title = "Second Shift", PublishDate = new DateOnly(2013, 4, 1), AuthorId = 5, Genre = BookGenre.ScienceFiction });
        context.SaveChanges();

        Assert.Equal(3, context.Books.Count(b => b.AuthorId == 5));
    }

    [Fact]
    public void ChangingForeignKeyOrNavigation_MovesBookToAnotherAuthor()
    {
        using var database = new SqliteDatabase();
        using (var context = database.CreateContext())
        {
            var murmur = context.Books.Single(b => b.BookId == 6);
            murmur.AuthorId = 6;

            var ishtar = context.Books.Include(b => b.Author).Single(b => b.BookId == 8);
            ishtar.Author = context.Authors.Single(a => a.AuthorId == 7);

            context.SaveChanges();
        }

        using (var context = database.CreateContext())
        {
            Assert.Equal(6, context.Books.Single(b => b.BookId == 6).AuthorId);
            Assert.Equal(7, context.Books.Single(b => b.BookId == 8).AuthorId);
        }
    }

    [Fact]
    public void RemovingBookFromRequiredCollection_DeletesTheOrphan()
    {
        using var database = new SqliteDatabase();
        using (var context = database.CreateContext())
        {
            var author = context.Authors.Include(a => a.Books).Single(a => a.AuthorId == 5);
            var wool = author.Books.Single(b => b.Title == "Wool");

            author.Books.Remove(wool);
            context.ChangeTracker.DetectChanges();

            Assert.Equal(EntityState.Deleted, context.Entry(wool).State);
            context.SaveChanges();
        }

        using (var context = database.CreateContext())
        {
            Assert.False(context.Books.Any(b => b.Title == "Wool"));
        }
    }

    [Fact]
    public void DeletingAuthor_CascadesToBooksCoversAndAssignments()
    {
        using var database = new SqliteDatabase();
        using (var context = database.CreateContext())
        {
            var author = context.Authors
                .Include(a => a.Books)
                .ThenInclude(b => b.Cover)
                .Single(a => a.AuthorId == 5);

            context.Authors.Remove(author);

            Assert.All(author.Books, book => Assert.Equal(EntityState.Deleted, context.Entry(book).State));
            Assert.Equal(EntityState.Deleted, context.Entry(author.Books.Single(b => b.BookId == 5).Cover!).State);
            context.SaveChanges();
        }

        using (var context = database.CreateContext())
        {
            Assert.False(context.Books.Any(b => b.AuthorId == 5));
            Assert.False(context.Covers.Any(c => c.CoverId == 4));
            Assert.False(context.Set<CoverAssignment>().Any(ca => ca.CoverId == 4));
            Assert.True(context.Artists.Any(a => a.ArtistId == 2));
        }
    }

    [Fact]
    public void DeletingTrackedPublisher_SetsBookPublisherToNull()
    {
        using var database = new SqliteDatabase();
        using (var context = database.CreateContext())
        {
            var publisher = context.Publishers.Include(p => p.Books).Single(p => p.PublisherId == 2);

            context.Publishers.Remove(publisher);
            context.SaveChanges();
        }

        using (var context = database.CreateContext())
        {
            int[] formerPublisherBooks = [2, 4, 5];
            Assert.Equal(3, context.Books.Count(b => formerPublisherBooks.Contains(b.BookId) && b.PublisherId == null));
            Assert.False(context.Publishers.Any(p => p.PublisherId == 2));
        }
    }

    [Fact]
    public void DeletingPublisherInDatabase_UsesOnDeleteSetNull()
    {
        using var database = new SqliteDatabase();
        using var context = database.CreateContext();

        context.Publishers.Where(p => p.PublisherId == 1).ExecuteDelete();

        Assert.Equal(9, context.Books.Count());
        Assert.Equal(6, context.Books.Count(b => b.PublisherId == null));
    }

    [Fact]
    public void RequiredRelationship_RejectsBookWithUnknownAuthor()
    {
        using var database = new SqliteDatabase();
        using var context = database.CreateContext();

        context.Books.Add(new Book { Title = "Orphan", PublishDate = new DateOnly(2020, 1, 1), AuthorId = 999 });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
