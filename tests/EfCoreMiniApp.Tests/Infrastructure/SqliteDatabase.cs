using EfCoreMiniApp.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EfCoreMiniApp.Tests.Infrastructure;

public sealed class SqliteDatabase : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public SqliteDatabase()
    {
        _connection.Open();
        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public PubContext CreateContext(Action<DbContextOptionsBuilder<PubContext>>? configure = null)
    {
        var builder = new DbContextOptionsBuilder<PubContext>().UseSqlite(_connection);
        configure?.Invoke(builder);
        return new PubContext(builder.Options);
    }

    public void Dispose() => _connection.Dispose();
}
