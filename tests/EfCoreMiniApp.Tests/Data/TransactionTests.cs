using System.ComponentModel.DataAnnotations;
using EfCoreMiniApp.Domain;
using EfCoreMiniApp.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EfCoreMiniApp.Tests.Data;

public class TransactionTests
{
    [Fact]
    public async Task CommitAsync_PersistsEveryStepOfTheTransaction()
    {
        using var database = new SqliteDatabase();
        await using (var context = database.CreateContext())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();

            context.Authors.Add(new Author { FirstName = "Lynda", LastName = "Rutledge" });
            await context.SaveChangesAsync();

            context.Publishers.Add(new Publisher { Name = "Giraffe Books" });
            await context.SaveChangesAsync();

            await transaction.CommitAsync();
        }

        await using (var context = database.CreateContext())
        {
            Assert.True(await context.Authors.AnyAsync(a => a.LastName == "Rutledge"));
            Assert.True(await context.Publishers.AnyAsync(p => p.Name == "Giraffe Books"));
        }
    }

    [Fact]
    public async Task RollbackAsync_DiscardsSavedChanges()
    {
        using var database = new SqliteDatabase();
        await using (var context = database.CreateContext())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();

            context.Authors.Add(new Author { FirstName = "Lynda", LastName = "Rutledge" });
            await context.SaveChangesAsync();
            Assert.True(await context.Authors.AnyAsync(a => a.LastName == "Rutledge"));

            await transaction.RollbackAsync();
        }

        await using (var context = database.CreateContext())
        {
            Assert.False(await context.Authors.AnyAsync(a => a.LastName == "Rutledge"));
        }
    }

    [Fact]
    public async Task FailureInsideTransaction_RollsBackEarlierBulkDelete()
    {
        using var database = new SqliteDatabase();
        await using (var context = database.CreateContext())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var artists = await context.Artists.Where(a => a.Covers.Any(c => c.BookId == 3)).ToListAsync();
                artists.ForEach(artist => artist.Notes = new string('x', 600));

                await context.Books.Where(b => b.BookId == 3).ExecuteDeleteAsync();
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (ValidationException)
            {
                await transaction.RollbackAsync();
            }
        }

        await using (var context = database.CreateContext())
        {
            Assert.True(await context.Books.AnyAsync(b => b.BookId == 3));
            Assert.True(await context.Covers.AnyAsync(c => c.BookId == 3));
        }
    }

    [Fact]
    public async Task BulkDeleteWithoutTransaction_IsCommittedImmediately()
    {
        using var database = new SqliteDatabase();
        await using (var context = database.CreateContext())
        {
            var artists = await context.Artists.Where(a => a.Covers.Any(c => c.BookId == 3)).ToListAsync();
            artists.ForEach(artist => artist.Notes = new string('x', 600));

            await context.Books.Where(b => b.BookId == 3).ExecuteDeleteAsync();
            await Assert.ThrowsAsync<ValidationException>(() => context.SaveChangesAsync());
        }

        await using (var context = database.CreateContext())
        {
            Assert.False(await context.Books.AnyAsync(b => b.BookId == 3));
        }
    }
}
