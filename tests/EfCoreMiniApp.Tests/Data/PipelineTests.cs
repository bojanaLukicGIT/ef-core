using System.ComponentModel.DataAnnotations;
using EfCoreMiniApp.Data;
using EfCoreMiniApp.Data.Interceptors;
using EfCoreMiniApp.Domain;
using EfCoreMiniApp.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EfCoreMiniApp.Tests.Data;

public class PipelineTests
{
    [Fact]
    public void SaveChangesInterceptor_SetsAuditShadowPropertiesOnInsert()
    {
        using var database = new SqliteDatabase();
        using var context = database.CreateContext();
        var before = DateTime.UtcNow;
        var author = new Author { FirstName = "Deirdre", LastName = "Sinnott" };

        context.Authors.Add(author);
        context.SaveChanges();

        var createdAt = context.Entry(author).Property<DateTime>(AuditInterceptor.CreatedAt).CurrentValue;
        var lastUpdated = context.Entry(author).Property<DateTime>(AuditInterceptor.LastUpdated).CurrentValue;
        Assert.InRange(createdAt, before, DateTime.UtcNow);
        Assert.Equal(createdAt, lastUpdated);
    }

    [Fact]
    public void SaveChangesInterceptor_UpdatesLastUpdatedAndKeepsCreatedAt()
    {
        using var database = new SqliteDatabase();
        using (var context = database.CreateContext())
        {
            var book = context.Books.Find(1)!;
            book.BasePrice = 13.99m;
            context.SaveChanges();
        }

        using (var context = database.CreateContext())
        {
            var audit = context.Books
                .Where(b => b.BookId == 1)
                .Select(b => new
                {
                    CreatedAt = EF.Property<DateTime>(b, AuditInterceptor.CreatedAt),
                    LastUpdated = EF.Property<DateTime>(b, AuditInterceptor.LastUpdated)
                })
                .Single();

            Assert.Equal(SeedData.SeedDate, audit.CreatedAt);
            Assert.True(audit.LastUpdated > SeedData.SeedDate);
        }
    }

    [Fact]
    public void AttachingDetachedEntityAsModified_DoesNotOverwriteCreatedAt()
    {
        using var database = new SqliteDatabase();
        var detached = new Publisher { PublisherId = 1, Name = "Blue Harbor Books International", FoundedYear = 1952 };

        using (var context = database.CreateContext())
        {
            context.Entry(detached).State = EntityState.Modified;
            context.SaveChanges();
        }

        using (var context = database.CreateContext())
        {
            var createdAt = context.Publishers
                .Where(p => p.PublisherId == 1)
                .Select(p => EF.Property<DateTime>(p, AuditInterceptor.CreatedAt))
                .Single();

            Assert.Equal(SeedData.SeedDate, createdAt);
            Assert.Equal("Blue Harbor Books International", context.Publishers.Find(1)!.Name);
        }
    }

    [Fact]
    public void ChangingOwnedJsonEntity_UpdatesTheOwnersAuditData()
    {
        using var database = new SqliteDatabase();
        using (var context = database.CreateContext())
        {
            var author = context.Authors.Find(2)!;
            author.Contact = new ContactDetails { EmailAddress = "ruth@example.com" };
            context.SaveChanges();
        }

        using (var context = database.CreateContext())
        {
            var lastUpdated = context.Authors
                .Where(a => a.AuthorId == 2)
                .Select(a => EF.Property<DateTime>(a, AuditInterceptor.LastUpdated))
                .Single();

            Assert.True(lastUpdated > SeedData.SeedDate);
            Assert.Equal("ruth@example.com", context.Authors.Find(2)!.Contact!.EmailAddress);
        }
    }

    [Fact]
    public void ShadowProperties_CanBeUsedInQueries()
    {
        using var database = new SqliteDatabase();
        using (var context = database.CreateContext())
        {
            context.Authors.Find(6)!.FirstName = "Isabel";
            context.SaveChanges();
        }

        using (var context = database.CreateContext())
        {
            var recentlyUpdated = context.Authors
                .Where(a => EF.Property<DateTime>(a, AuditInterceptor.LastUpdated) > SeedData.SeedDate)
                .Select(a => a.AuthorId)
                .ToList();

            Assert.Equal([6], recentlyUpdated);
        }
    }

    [Fact]
    public void SavingChangesAndSavedChangesEvents_CaptureASummary()
    {
        using var database = new SqliteDatabase();
        using var context = database.CreateContext();

        context.Authors.Add(new Author { FirstName = "New", LastName = "Author" });
        context.Authors.Find(1)!.FirstName = "Rhoda B.";
        context.Authors.Remove(context.Authors.Find(7)!);

        context.SaveChanges();

        Assert.Equal(new SaveChangesSummary(Added: 1, Modified: 1, Deleted: 1, EntitiesSaved: 3), context.LastSaveSummary);
    }

    [Fact]
    public void SaveChangesOverride_ValidatesDataAnnotationsBeforeSaving()
    {
        using var database = new SqliteDatabase();
        using (var context = database.CreateContext())
        {
            context.Authors.Add(new Author { FirstName = new string('x', 150), LastName = "TooLong" });

            var exception = Assert.Throws<ValidationException>(() => context.SaveChanges());
            Assert.Contains("FirstName", exception.Message);
        }

        using (var context = database.CreateContext())
        {
            Assert.False(context.Authors.Any(a => a.LastName == "TooLong"));
        }
    }

    [Fact]
    public async Task SaveChangesAsyncOverride_ValidatesRequiredFields()
    {
        using var database = new SqliteDatabase();
        await using var context = database.CreateContext();

        context.Publishers.Add(new Publisher { Name = "" });

        await Assert.ThrowsAsync<ValidationException>(() => context.SaveChangesAsync());
        Assert.Null(context.LastSaveSummary);
    }

    [Fact]
    public void CommandInterceptor_LeavesNonSqlServerCommandsUnchanged()
    {
        using var database = new SqliteDatabase();
        var log = new List<string>();
        using var context = database.CreateContext(options =>
            options.LogTo(log.Add, [DbLoggerCategory.Database.Command.Name], LogLevel.Information));

        var authors = context.Authors.TagWith(QueryHintCommandInterceptor.RobustPlanTag).ToList();

        Assert.Equal(7, authors.Count);
        Assert.Contains(log, entry => entry.Contains($"-- {QueryHintCommandInterceptor.RobustPlanTag}"));
        Assert.DoesNotContain(log, entry => entry.Contains("ROBUST PLAN"));
    }
}
