using System.ComponentModel.DataAnnotations;

namespace QueryPush.Configuration;

public class EndpointConfig
{
    [Required, MinLength(1)]
    public string Name { get; set; } = string.Empty;
    
    public HttpMethodType Method { get; set; } = HttpMethodType.POST;
    
    [Required, Url]
    public string Url { get; set; } = string.Empty;
    
    public HeaderConfig[] Headers { get; set; } = [];
    
    [Range(0, 10)]
    public int RetryAttempts { get; set; } = 3;
    
    public RetryStrategyType RetryStrategy { get; set; } = RetryStrategyType.Delay;
    
    [Range(1, 300)]
    public int BackOffSeconds { get; set; } = 15;
    
    public bool SendRequestIfNoResults { get; set; } = false;
    
    [Range(1, int.MaxValue)]
    public int PayloadSize { get; set; } = int.MaxValue;
    
    [Range(0, 10000)]
    public int RequestDelay { get; set; } = 500;
}