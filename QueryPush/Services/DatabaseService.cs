using System.Data;
using System.Data.Odbc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using QueryPush.Configuration;

namespace QueryPush.Services;

public interface IDatabaseService
{
    Task<IEnumerable<Dictionary<string, object>>> ExecuteQueryAsync(string databaseName, string query, int timeoutSeconds, int maxRows);
}

public class DatabaseService(
    IOptionsMonitor<QueryPushSettings> options,
    ILogger<DatabaseService> logger) : IDatabaseService
{
    public async Task<IEnumerable<Dictionary<string, object>>> ExecuteQueryAsync(string databaseName, string query, int timeoutSeconds, int maxRows)
    {
        var database = options.CurrentValue.Databases.FirstOrDefault(d => d.Name == databaseName);
        if (database == null)
        {
            logger.LogError("Database '{DatabaseName}' not found in configuration", databaseName);
            throw new InvalidOperationException($"Database '{databaseName}' not found");
        }

        logger.LogInformation("Executing query on database '{DatabaseName}' (timeout: {TimeoutSeconds}s, max rows: {MaxRows})", 
            databaseName, timeoutSeconds, maxRows);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var results = await ExecuteOdbcQueryAsync(database, query, timeoutSeconds, maxRows);

            stopwatch.Stop();
            var resultCount = results.Count();
            logger.LogInformation("Query completed successfully. Returned {ResultCount} rows in {ElapsedMs}ms", 
                resultCount, stopwatch.ElapsedMilliseconds);

            return results;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Query execution failed on database '{DatabaseName}' after {ElapsedMs}ms", 
                databaseName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private async Task<IEnumerable<Dictionary<string, object>>> ExecuteOdbcQueryAsync(DatabaseConfig database, string query, int timeoutSeconds, int maxRows)
    {
        logger.LogDebug("Connecting to database '{DatabaseName}' via ODBC", database.Name);
        
        await using var connection = new OdbcConnection(database.ConnectionString);
        await connection.OpenAsync();
        logger.LogDebug("Connected to database '{DatabaseName}'", database.Name);
        
        await using var command = new OdbcCommand(query, connection) { CommandTimeout = timeoutSeconds };
        await using var reader = await command.ExecuteReaderAsync();
        return ReadResults(reader, maxRows);
    }

    private List<Dictionary<string, object>> ReadResults(IDataReader reader, int maxRows)
    {
        var results = new List<Dictionary<string, object>>();
        var rowCount = 0;

        while (reader.Read() && rowCount < maxRows)
        {
            var row = new Dictionary<string, object>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var value = reader.GetValue(i);
                var fieldName = reader.GetName(i) ?? $"Column{i}";
                row[fieldName] = value == DBNull.Value ? null! : value;
            }
            results.Add(row);
            rowCount++;
        }

        logger.LogDebug("Read {RowCount} rows from database result set", rowCount);
        return results;
    }
}
