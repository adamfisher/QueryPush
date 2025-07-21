using Microsoft.Extensions.Logging;
using QueryPush.Configuration;

namespace QueryPush.Services;

public interface IQueryTextResolver
{
    Task<string> GetQueryTextAsync(QueryConfig query);
}

public class QueryTextResolver(ILogger<QueryTextResolver> logger) : IQueryTextResolver
{
    public async Task<string> GetQueryTextAsync(QueryConfig query)
    {
        if (!string.IsNullOrEmpty(query.QueryText))
        {
            logger.LogDebug("Using inline QueryText for '{QueryName}'", query.Name);
            return query.QueryText;
        }

        if (!string.IsNullOrEmpty(query.QueryFile))
        {
            logger.LogDebug("Reading QueryFile '{QueryFile}' for '{QueryName}'", query.QueryFile, query.Name);
            var queryText = await File.ReadAllTextAsync(query.QueryFile);
            logger.LogDebug("Read {CharacterCount} characters from '{QueryFile}'", queryText.Length, query.QueryFile);
            return queryText;
        }

        throw new InvalidOperationException($"Query '{query.Name}' has neither QueryText nor QueryFile specified");
    }
}
