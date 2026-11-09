using EfCoreMiniApp.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EfCoreMiniApp.Tests.Data;

public class LoggingTests
{
    [Fact]
    public void LogTo_WithCategoryAndLevelFilter_CapturesOnlyExecutedSql()
    {
        using var database = new SqliteDatabase();
        var log = new List<string>();
        using var context = database.CreateContext(options =>
            options.LogTo(log.Add, [DbLoggerCategory.Database.Command.Name], LogLevel.Information));

        _ = context.Authors.Where(a => a.LastName == "Ozeki").ToList();

        var entry = Assert.Single(log);
        Assert.Contains("Executed DbCommand", entry);
        Assert.Contains("FROM \"Authors\"", entry);
    }

    [Fact]
    public void SensitiveDataLogging_ShowsParameterValues()
    {
        using var database = new SqliteDatabase();
        var hiddenLog = new List<string>();
        var visibleLog = new List<string>();
        var lastName = "Howey";

        using (var context = database.CreateContext(options =>
                   options.LogTo(hiddenLog.Add, [DbLoggerCategory.Database.Command.Name], LogLevel.Information)))
        {
            _ = context.Authors.Where(a => a.LastName == lastName).ToList();
        }

        using (var context = database.CreateContext(options => options
                   .LogTo(visibleLog.Add, [DbLoggerCategory.Database.Command.Name], LogLevel.Information)
                   .EnableSensitiveDataLogging()))
        {
            _ = context.Authors.Where(a => a.LastName == lastName).ToList();
        }

        Assert.DoesNotContain(hiddenLog, entry => entry.Contains("'Howey'"));
        Assert.Contains(visibleLog, entry => entry.Contains("'Howey'"));
    }

    [Fact]
    public void ToQueryString_ShowsSqlWithoutExecutingTheQuery()
    {
        using var database = new SqliteDatabase();
        var log = new List<string>();
        using var context = database.CreateContext(options =>
            options.LogTo(log.Add, [DbLoggerCategory.Database.Command.Name], LogLevel.Information));

        var sql = context.Books
            .Where(b => b.PublishDate.Year > 2000)
            .OrderBy(b => b.Title)
            .Select(b => b.Title)
            .ToQueryString();

        Assert.Contains("SELECT \"b\".\"BookTitle\"", sql);
        Assert.Contains("ORDER BY", sql);
        Assert.Empty(log);
    }

    [Fact]
    public void TagWith_AddsCommentToGeneratedSql()
    {
        using var database = new SqliteDatabase();
        using var context = database.CreateContext();

        var sql = context.Authors.TagWith("Authors for the report").ToQueryString();

        Assert.StartsWith("-- Authors for the report", sql);
    }
}
