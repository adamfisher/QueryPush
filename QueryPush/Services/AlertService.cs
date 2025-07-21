using System.Net.Mail;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using QueryPush.Configuration;

namespace QueryPush.Services;

public interface IAlertService
{
    Task SendSlackAlertAsync(QueryConfig query, string queryText, Exception exception);
    Task SendEmailAlertAsync(QueryConfig query, string queryText, Exception exception);
    bool CanSendAlert(string queryName, string alertType);
}

public class AlertService(
    IOptionsMonitor<QueryPushSettings> options, 
    IStateManager stateManager, 
    HttpClient httpClient,
    ILogger<AlertService> logger)
    : IAlertService
{
    public async Task SendSlackAlertAsync(QueryConfig query, string queryText, Exception exception)
    {
        if (!CanSendAlert(query.Name, "Slack"))
        {
            logger.LogDebug("Skipping Slack alert for '{QueryName}' due to cooldown period", query.Name);
            return;
        }

        var slackConfig = options.CurrentValue.Alerts.Slack;
        if (slackConfig?.WebhookUrl == null)
        {
            logger.LogWarning("Slack alert requested for '{QueryName}' but Slack configuration is missing", query.Name);
            return;
        }

        logger.LogInformation("Sending Slack alert for query '{QueryName}' to channel '{Channel}'", query.Name, slackConfig.Channel);

        var payload = new
        {
            channel = slackConfig.Channel,
            username = slackConfig.Username,
            blocks = new object[]
            {
                new
                {
                    type = "header",
                    text = new { type = "plain_text", text = $"🚨 QueryPush Failure: {query.Name}" }
                },
                new
                {
                    type = "section",
                    fields = new object[]
                    {
                        new { type = "mrkdwn", text = $"*Database:*\n{query.Database}" },
                        new { type = "mrkdwn", text = $"*Endpoint:*\n{query.Endpoint}" },
                        new { type = "mrkdwn", text = $"*Schedule:*\n`{query.Cron}`" },
                        new { type = "mrkdwn", text = $"*Time:*\n{DateTime.Now:yyyy-MM-dd HH:mm:ss}" }
                    }
                },
                new
                {
                    type = "section",
                    text = new { type = "mrkdwn", text = $"*Query:*\n```sql\n{queryText}\n```" }
                },
                new
                {
                    type = "section",
                    text = new { type = "mrkdwn", text = $"*Error:*\n```\n{exception.Message}\n```" }
                },
                new
                {
                    type = "context",
                    elements = new object[]
                    {
                        new { type = "mrkdwn", text = $"Exception Type: `{exception.GetType().Name}`" }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        try
        {
            var response = await httpClient.PostAsync(slackConfig.WebhookUrl, content);
            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Slack alert sent successfully for '{QueryName}'", query.Name);
                stateManager.SetLastAlert(query.Name, "Slack", DateTime.Now);
                await stateManager.SaveAsync();
            }
            else
            {
                logger.LogError("Slack webhook returned {StatusCode}: {ReasonPhrase}", (int)response.StatusCode, response.ReasonPhrase);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send Slack alert for '{QueryName}'", query.Name);
            throw;
        }
    }

    public async Task SendEmailAlertAsync(QueryConfig query, string queryText, Exception exception)
    {
        if (!CanSendAlert(query.Name, "Email"))
        {
            logger.LogDebug("Skipping email alert for '{QueryName}' due to cooldown period", query.Name);
            return;
        }

        var emailConfig = options.CurrentValue.Alerts.Email;
        if (emailConfig?.SmtpHost == null)
        {
            logger.LogWarning("Email alert requested for '{QueryName}' but email configuration is missing", query.Name);
            return;
        }

        logger.LogInformation("Sending email alert for query '{QueryName}' to {EmailTo}", query.Name, emailConfig.To);

        using var smtp = new SmtpClient(emailConfig.SmtpHost, emailConfig.SmtpPort)
        {
            EnableSsl = emailConfig.UseSsl,
            Credentials = new System.Net.NetworkCredential(emailConfig.Username, emailConfig.Password)
        };

        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ background-color: #dc3545; color: white; padding: 15px; border-radius: 5px; margin-bottom: 20px; }}
        .section {{ margin-bottom: 20px; }}
        .label {{ font-weight: bold; color: #495057; }}
        .code {{ background-color: #f8f9fa; border: 1px solid #dee2e6; padding: 10px; border-radius: 5px; font-family: monospace; white-space: pre-wrap; }}
        .error {{ background-color: #f8d7da; border: 1px solid #f5c6cb; color: #721c24; padding: 10px; border-radius: 5px; }}
        .details {{ background-color: #e2e3e5; padding: 10px; border-radius: 5px; }}
    </style>
</head>
<body>
    <div class=""header"">
        <h2>🚨 QueryPush Failure: {query.Name}</h2>
    </div>
    
    <div class=""section"">
        <div class=""details"">
            <p><span class=""label"">Database:</span> {query.Database}</p>
            <p><span class=""label"">Endpoint:</span> {query.Endpoint}</p>
            <p><span class=""label"">Schedule:</span> {query.Cron}</p>
            <p><span class=""label"">Failure Time:</span> {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
        </div>
    </div>

    <div class=""section"">
        <h3>Query</h3>
        <div class=""code"">{System.Web.HttpUtility.HtmlEncode(queryText)}</div>
    </div>

    <div class=""section"">
        <h3>Exception Details</h3>
        <div class=""error"">
            <p><span class=""label"">Type:</span> {exception.GetType().Name}</p>
            <p><span class=""label"">Message:</span> {System.Web.HttpUtility.HtmlEncode(exception.Message)}</p>
        </div>
        {(exception.StackTrace != null ? $@"
        <h4>Stack Trace</h4>
        <div class=""code"">{System.Web.HttpUtility.HtmlEncode(exception.StackTrace)}</div>" : "")}
    </div>
</body>
</html>";

        var mailMessage = new MailMessage(emailConfig.From, emailConfig.To)
        {
            Subject = $"QueryPush Failure for {query.Name} Query",
            Body = htmlBody,
            IsBodyHtml = true
        };

        try
        {
            await smtp.SendMailAsync(mailMessage);
            logger.LogInformation("Email alert sent successfully for '{QueryName}'", query.Name);
            stateManager.SetLastAlert(query.Name, "Email", DateTime.Now);
            await stateManager.SaveAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email alert for '{QueryName}' to {EmailTo}", query.Name, emailConfig.To);
            throw;
        }
    }

    public bool CanSendAlert(string queryName, string alertType)
    {
        var lastAlert = stateManager.GetLastAlert(queryName, alertType);
        if (lastAlert == null)
        {
            logger.LogDebug("No previous {AlertType} alert found for '{QueryName}', allowing alert", alertType, queryName);
            return true;
        }

        var cooldownMinutes = alertType == "Slack" 
            ? options.CurrentValue.Alerts.Slack?.AlertCooldownMinutes ?? 60
            : options.CurrentValue.Alerts.Email?.AlertCooldownMinutes ?? 60;

        var canSend = DateTime.Now > lastAlert.Value.AddMinutes(cooldownMinutes);
        var remainingCooldown = lastAlert.Value.AddMinutes(cooldownMinutes) - DateTime.Now;

        if (!canSend)
        {
            logger.LogDebug("{AlertType} alert for '{QueryName}' is in cooldown for {RemainingMinutes} more minutes", 
                alertType, queryName, Math.Ceiling(remainingCooldown.TotalMinutes));
        }

        return canSend;
    }
}
