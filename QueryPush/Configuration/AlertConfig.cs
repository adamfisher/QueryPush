namespace QueryPush.Configuration;

public class AlertConfig
{
    public SlackAlertConfig? Slack { get; set; }
    public EmailAlertConfig? Email { get; set; }
}