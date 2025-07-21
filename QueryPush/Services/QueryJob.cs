using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using Serilog.Context;
using QueryPush.Configuration;

namespace QueryPush.Services;

[DisallowConcurrentExecution]
public class QueryJob(
    IQueryExecutor queryExecutor,
    IOptionsMonitor<QueryPushSettings> options,
    ILogger<QueryJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var queryName = context.MergedJobDataMap.GetString("queryName")!;
        var query = options.CurrentValue.Queries.FirstOrDefault(q => q.Name == queryName);
        
        if (query == null)
        {
            logger.LogError("Query '{QueryName}' not found in configuration", queryName);
            return;
        }
        
        var correlationId = GenerateShortId();
        
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            var executionStartTime = DateTime.Now;
            try
            {
                logger.LogDebug("Starting execution of query '{QueryName}'", query.Name);
                await queryExecutor.ExecuteQueryAsync(query);
                
                var executionDuration = DateTime.Now - executionStartTime;
                logger.LogInformation("Query '{QueryName}' completed successfully in {ElapsedSeconds:F1} seconds", 
                    query.Name, executionDuration.TotalSeconds);
            }
            catch (Exception ex)
            {
                var executionDuration = DateTime.Now - executionStartTime;
                logger.LogError(ex, "Query '{QueryName}' failed after {ElapsedSeconds:F1} seconds", 
                    query.Name, executionDuration.TotalSeconds);
            }
        }
    }

    private static string GenerateShortId()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("/", "_")
            .Replace("+", "-")
            .Substring(0, 8);
    }
}
