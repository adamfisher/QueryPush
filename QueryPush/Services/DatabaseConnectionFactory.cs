using System.Data.Common;
using System.Data.Odbc;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using Microsoft.Extensions.Logging;
using QueryPush.Configuration;

namespace QueryPush.Services;

/// <summary>
/// Factory for creating database connections based on provider configuration.
/// Supports multiple database providers including ODBC, SQL Server, MySQL, Oracle, PostgreSQL, and SQLite.
/// </summary>
public class DatabaseConnectionFactory : IDatabaseConnectionFactory
{
    private readonly ILogger<DatabaseConnectionFactory> _logger;

    public DatabaseConnectionFactory(ILogger<DatabaseConnectionFactory> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates and opens a database connection asynchronously.
    /// </summary>
    public async Task<DbConnection> CreateConnectionAsync(DatabaseConfig config)
    {
        var connection = CreateConnection(config);

        _logger.LogDebug("Opening connection to database '{DatabaseName}' using provider '{Provider}'",
            config.Name, config.Provider);

        await connection.OpenAsync();

        _logger.LogDebug("Successfully opened connection to database '{DatabaseName}'", config.Name);

        return connection;
    }

    /// <summary>
    /// Creates a database connection without opening it.
    /// </summary>
    public DbConnection CreateConnection(DatabaseConfig config)
    {
        _logger.LogDebug("Creating {Provider} connection for database '{DatabaseName}'",
            config.Provider, config.Name);

        DbConnection connection = config.Provider.ToLowerInvariant() switch
        {
            "odbc" => new OdbcConnection(config.ConnectionString),
            "sqlserver" => new SqlConnection(config.ConnectionString),
            "mysql" => new MySqlConnection(config.ConnectionString),
            "oracle" => new OracleConnection(config.ConnectionString),
            "postgres" or "postgresql" => new NpgsqlConnection(config.ConnectionString),
            "sqlite" => new SqliteConnection(config.ConnectionString),
            _ => throw new NotSupportedException(
                $"Database provider '{config.Provider}' is not supported. " +
                $"Supported providers: odbc, sqlserver, mysql, oracle, postgres, sqlite")
        };

        return connection;
    }
}
