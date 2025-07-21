using NCrontab;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using QueryPush.Configuration;

namespace QueryPush.Services;

public interface IQueryScheduler
{
    Task<IEnumerable<QueryConfig>> GetDueQueriesAsync();
    DateTime GetNextRun(QueryConfig query);
}

public class QueryScheduler(
    IOptionsMonitor<QueryPushSettings> options, 
    IStateManager stateManager,
    ILogger<QueryScheduler> logger)
    : IQueryScheduler
{
    public Task<IEnumerable<QueryConfig>> GetDueQueriesAsync()
    {
        var settings = options.CurrentValue;
        var now = DateTime.Now;
        var dueQueries = new List<QueryConfig>();

        logger.LogDebug("Checking {QueryCount} enabled queries for execution at {CurrentTime}", 
            settings.Queries.Count(q => q.Enabled), now);

        foreach (var query in settings.Queries.Where(q => q.Enabled))
        {
            if (IsQueryDue(query, now))
            {
                dueQueries.Add(query);
                logger.LogInformation("Query '{QueryName}' is due for execution", query.Name);
            }
            else
            {
                var nextRun = GetNextRun(query);
                logger.LogDebug("Query '{QueryName}' next run scheduled for {NextRun}", query.Name, nextRun);
            }
        }

        if (dueQueries.Count == 0)
        {
            logger.LogDebug("No queries are due for execution");
        }
        else
        {
            logger.LogInformation("Found {DueQueryCount} queries due for execution", dueQueries.Count);
        }

        return Task.FromResult<IEnumerable<QueryConfig>>(dueQueries);
    }

    public DateTime GetNextRun(QueryConfig query)
    {
        var schedule = CrontabSchedule.Parse(query.Cron);
        return schedule.GetNextOccurrence(DateTime.Now);
    }

    private bool IsQueryDue(QueryConfig query, DateTime now)
    {
        try
        {
            var schedule = CrontabSchedule.Parse(query.Cron);
            var lastRun = stateManager.GetLastRun(query.Name);
            
            if (lastRun == null)
            {
                var willRun = query.RunOnStartup;
                logger.LogDebug("Query '{QueryName}' has never run. RunOnStartup={RunOnStartup}, will execute={WillExecute}", 
                    query.Name, query.RunOnStartup, willRun);
                return willRun;
            }

            var nextRun = schedule.GetNextOccurrence(lastRun.Value);
            var isDue = now >= nextRun;
            
            logger.LogDebug("Query '{QueryName}' last run: {LastRun}, next run: {NextRun}, is due: {IsDue}", 
                query.Name, lastRun.Value, nextRun, isDue);
                
            return isDue;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error parsing cron expression '{CronExpression}' for query '{QueryName}'", 
                query.Cron, query.Name);
            return false;
        }
    }
}
