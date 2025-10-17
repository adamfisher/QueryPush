using System.Data.Common;
using QueryPush.Configuration;

namespace QueryPush.Services;

/// <summary>
/// Factory for creating database connections based on provider configuration.
/// </summary>
public interface IDatabaseConnectionFactory
{
    /// <summary>
    /// Creates and opens a database connection asynchronously.
    /// </summary>
    /// <param name="config">Database configuration containing provider and connection string.</param>
    /// <returns>An opened DbConnection instance.</returns>
    Task<DbConnection> CreateConnectionAsync(DatabaseConfig config);

    /// <summary>
    /// Creates a database connection (without opening it).
    /// </summary>
    /// <param name="config">Database configuration containing provider and connection string.</param>
    /// <returns>A DbConnection instance.</returns>
    DbConnection CreateConnection(DatabaseConfig config);
}
