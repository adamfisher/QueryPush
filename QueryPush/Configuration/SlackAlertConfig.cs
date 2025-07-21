using System.ComponentModel.DataAnnotations;

namespace QueryPush.Configuration;

public class SlackAlertConfig
{
    public bool Default { get; set; } = false;
    
    [Required, Url]
    public string WebhookUrl { get; set; } = string.Empty;
    
    public string Channel { get; set; } = "#alerts";
    public string Username { get; set; } = "QueryPush";
    
    [Range(1, 1440)]
    public int AlertCooldownMinutes { get; set; } = 60;
}