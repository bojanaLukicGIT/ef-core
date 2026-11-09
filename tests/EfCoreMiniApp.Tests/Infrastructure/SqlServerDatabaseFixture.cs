using EfCoreMiniApp.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace EfCoreMiniApp.Tests.Infrastructure;

public sealed class SqlServerDatabaseFixture : IDisposable
{
    public static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("EFCOREMINIAPP_TEST_SQLSERVER")
        ?? @"Server=(localdb)\MSSQLLocalDB;Database=EfCoreMiniAppTests;Trusted_Connection=True;TrustServerCertificate=True";

    public SqlServerDatabaseFixture()
    {
        using var context = CreateContext();
        context.Database.EnsureDeleted();
        context.Database.Migrate();
    }

    public PubContext CreateContext(Action<DbContextOptionsBuilder<PubContext>>? configure = null)
    {
        var builder = new DbContextOptionsBuilder<PubContext>().UseSqlServer(ConnectionString);
        configure?.Invoke(builder);
        return new PubContext(builder.Options);
    }

    public void Dispose()
    {
        using var context = CreateContext();
        context.Database.EnsureDeleted();
    }
}

[CollectionDefinition(Name)]
public class SqlServerCollection : ICollectionFixture<SqlServerDatabaseFixture>
{
    public const string Name = "SqlServer";
}

public class SqlServerApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PubConnection"] = SqlServerDatabaseFixture.ConnectionString
            }));
    }
}
