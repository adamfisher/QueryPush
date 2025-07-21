using System.ComponentModel.DataAnnotations;

namespace QueryPush.Configuration;

public class QueryConfig
{
    [Required, MinLength(1)]
    public string Name { get; set; } = string.Empty;
    
    [Required, MinLength(1)]
    public string Cron { get; set; } = string.Empty;
    
    [Required, MinLength(1)]
    public string Database { get; set; } = string.Empty;
    
    [Required, MinLength(1)]
    public string Endpoint { get; set; } = string.Empty;
    
    public bool Enabled { get; set; } = true;
    
    public bool RunOnStartup { get; set; } = true;
    
    [Range(1, 3600)]
    public int TimeoutSeconds { get; set; } = 30;
    
    [Range(1, int.MaxValue)]
    public int MaxRows { get; set; } = int.MaxValue;
    
    public PayloadFormatType PayloadFormat { get; set; } = PayloadFormatType.JsonArray;

    public FailureActionType OnFailure { get; set; } = FailureActionType.LogAndContinue;
    
    public string? QueryText { get; set; }
    
    public string? QueryFile { get; set; }
}
