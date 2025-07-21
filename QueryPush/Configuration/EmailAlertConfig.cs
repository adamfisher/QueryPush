using System.ComponentModel.DataAnnotations;

namespace QueryPush.Configuration;

public class EmailAlertConfig
{
    [Required, MinLength(1)]
    public string SmtpHost { get; set; } = string.Empty;
    
    [Range(1, 65535)]
    public int SmtpPort { get; set; } = 587;
    
    public bool UseSsl { get; set; } = true;
    
    [Required, EmailAddress]
    public string From { get; set; } = string.Empty;
    
    [Required, EmailAddress]
    public string To { get; set; } = string.Empty;
    
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    
    [Range(1, 1440)]
    public int AlertCooldownMinutes { get; set; } = 60;
}