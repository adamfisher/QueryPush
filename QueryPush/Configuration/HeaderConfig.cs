using System.ComponentModel.DataAnnotations;

namespace QueryPush.Configuration;

public class HeaderConfig
{
    [Required, MinLength(1)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public string Value { get; set; } = string.Empty;
}