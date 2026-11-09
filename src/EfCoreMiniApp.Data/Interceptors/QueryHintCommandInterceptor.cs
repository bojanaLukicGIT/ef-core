using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EfCoreMiniApp.Data.Interceptors;

public class QueryHintCommandInterceptor : DbCommandInterceptor
{
    public const string RobustPlanTag = "RobustPlan";
    public const string RobustPlanHint = " OPTION (ROBUST PLAN)";

    public static readonly QueryHintCommandInterceptor Instance = new();

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        AddRobustPlanHint(command, eventData);
        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        AddRobustPlanHint(command, eventData);
        return ValueTask.FromResult(result);
    }

    private static void AddRobustPlanHint(DbCommand command, CommandEventData eventData)
    {
        if (eventData.Context?.Database.IsSqlServer() == true
            && command.CommandText.StartsWith($"-- {RobustPlanTag}", StringComparison.Ordinal))
        {
            command.CommandText += RobustPlanHint;
        }
    }
}
