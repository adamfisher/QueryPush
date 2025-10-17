using System.ComponentModel.DataAnnotations;

namespace QueryPush.Configuration;

public class DatabaseConfig
{
    [Required, MinLength(1)]
    public string Name { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Database provider type. Supported values: odbc, sqlserver, mysql, oracle, postgres/postgresql, sqlite.
    /// Defaults to 'odbc' for backward compatibility.
    /// </summary>
    public string Provider { get; set; } = "odbc";
}