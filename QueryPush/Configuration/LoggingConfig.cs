using System.ComponentModel.DataAnnotations;

namespace QueryPush.Configuration;

public class LoggingConfig
{
    public LogRotationStrategy RotationStrategy { get; set; } = LogRotationStrategy.Daily;
    
    [Range(1, 365)]
    public int RetentionDays { get; set; } = 30;
    
    public string LogDirectory { get; set; } = "logs";
}