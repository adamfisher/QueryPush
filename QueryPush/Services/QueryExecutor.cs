using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QueryPush.Configuration;

namespace QueryPush.Services;

public interface IQueryExecutor
{
    Task ExecuteQueryAsync(QueryConfig query);
}

public class QueryExecutor(
    IDatabaseService databaseService,
    IHttpService httpService,
    IVariableReplacer variableReplacer,
    IQueryTextResolver queryTextResolver,
    IStateManager stateManager,
    IAlertService alertService,
    IOptionsMonitor<QueryPushSettings> options,
    ILogger<QueryExecutor> logger)
    : IQueryExecutor
{
    public async Task ExecuteQueryAsync(QueryConfig query)
    {
        var endpoint = GetEndpointConfig(query.Endpoint);
        
        logger.LogInformation("Starting execution of query '{QueryName}' (max attempts: {MaxAttempts})", 
            query.Name, endpoint.RetryAttempts + 1);

        var attempt = 0;
        Exception? lastException = null;

        while (attempt <= endpoint.RetryAttempts)
        {
            attempt++;
            try
            {
                await ExecuteSingleAttemptAsync(query, endpoint);
                logger.LogInformation("Query '{QueryName}' completed successfully on attempt {Attempt}", 
                    query.Name, attempt);
                
                stateManager.SetLastRun(query.Name, DateTime.Now);
                await stateManager.SaveAsync();
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                
                if (IsInvalidQuery(ex))
                {
                    logger.LogError(ex, "Query '{QueryName}' failed with invalid query error - skipping retries", query.Name);
                    break;
                }
                
                if (attempt <= endpoint.RetryAttempts)
                {
                    var delay = CalculateDelay(endpoint, attempt);
                    logger.LogWarning(ex, "Query '{QueryName}' failed on attempt {Attempt}/{MaxAttempts}. Retrying in {DelayMs}ms", 
                        query.Name, attempt, endpoint.RetryAttempts + 1, delay);
                    await Task.Delay(delay);
                }
                else
                {
                    logger.LogError(ex, "Query '{QueryName}' failed on final attempt {Attempt}/{MaxAttempts}", 
                        query.Name, attempt, endpoint.RetryAttempts + 1);
                }
            }
        }

        await HandleQueryFailureAsync(query, lastException!);
    }

    private EndpointConfig GetEndpointConfig(string endpointName)
    {
        var endpoint = options.CurrentValue.Endpoints.FirstOrDefault(e => e.Name == endpointName);
        if (endpoint == null)
            throw new InvalidOperationException($"Endpoint '{endpointName}' not found");
        return endpoint;
    }

    private static bool IsInvalidQuery(Exception ex)
    {
        var message = ex.Message.ToLowerInvariant();
        return message.Contains("syntax error") ||
               message.Contains("table") && message.Contains("doesn't exist") ||
               message.Contains("column") && message.Contains("doesn't exist") ||
               message.Contains("unknown column") ||
               message.Contains("unknown table") ||
               message.Contains("invalid object name") ||
               message.Contains("incorrect syntax") ||
               message.Contains("driver") && message.Contains("not found");
    }

    private async Task ExecuteSingleAttemptAsync(QueryConfig query, EndpointConfig endpoint)
    {
        logger.LogDebug("Executing single attempt for query '{QueryName}'", query.Name);
        
        var queryText = await queryTextResolver.GetQueryTextAsync(query);
        var processedQuery = variableReplacer.Replace(queryText, query.Name, stateManager);
        var results = await databaseService.ExecuteQueryAsync(query.Database, processedQuery, query.TimeoutSeconds, query.MaxRows);
        
        var resultCount = results.Count();
        logger.LogDebug("Database query returned {ResultCount} rows", resultCount);
        
        if (!results.Any() && !endpoint.SendRequestIfNoResults)
        {
            logger.LogInformation("Query '{QueryName}' returned no results and SendRequestIfNoResults=false, skipping HTTP request", 
                query.Name);
            return;
        }

        logger.LogDebug("Sending {ResultCount} rows to endpoint '{EndpointName}'", resultCount, query.Endpoint);
        await httpService.SendDataAsync(query.Endpoint, results, query.PayloadFormat);
    }

    private int CalculateDelay(EndpointConfig endpoint, int attempt)
    {
        var delay = endpoint.RetryStrategy switch
        {
            RetryStrategyType.ExponentialBackoff => (int)(endpoint.BackOffSeconds * 1000 * Math.Pow(2, attempt - 1)),
            _ => endpoint.BackOffSeconds * 1000
        };

        logger.LogDebug("Calculated retry delay using {RetryStrategy}: {DelayMs}ms", 
            endpoint.RetryStrategy, delay);

        return delay;
    }

    private async Task HandleQueryFailureAsync(QueryConfig query, Exception exception)
    {
        logger.LogError(exception, "Query '{QueryName}' exhausted all retry attempts. Handling failure with strategy: {FailureStrategy}", 
            query.Name, query.OnFailure);

        var queryText = await queryTextResolver.GetQueryTextAsync(query);

        switch (query.OnFailure)
        {
            case FailureActionType.SlackAlert:
                await alertService.SendSlackAlertAsync(query, queryText, exception);
                break;
            case FailureActionType.EmailAlert:
                await alertService.SendEmailAlertAsync(query, queryText, exception);
                break;
            case FailureActionType.Halt:
                logger.LogCritical("Query '{QueryName}' configured to halt on failure, terminating execution", query.Name);
                throw exception;
            case FailureActionType.LogAndContinue:
                logger.LogWarning("Query '{QueryName}' failed but configured to continue execution", query.Name);
                break;
        }
    }
}
