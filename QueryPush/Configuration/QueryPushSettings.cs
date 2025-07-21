using System.ComponentModel.DataAnnotations;

namespace QueryPush.Configuration;

public class QueryPushSettings
{
    [Required]
    [MinLength(1, ErrorMessage = "At least one database configuration is required")]
    public DatabaseConfig[] Databases { get; set; } = [];
    
    [Required]
    [MinLength(1, ErrorMessage = "At least one endpoint configuration is required")]
    public EndpointConfig[] Endpoints { get; set; } = [];
    
    public AlertConfig Alerts { get; set; } = new();
    public LoggingConfig Logging { get; set; } = new();
    public QueryConfig[] Queries { get; set; } = [];
}