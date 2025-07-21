using System.ComponentModel.DataAnnotations;
using System.Data.Odbc;
using Microsoft.Extensions.Options;
using QueryPush.Configuration;

namespace QueryPush.Services;

public interface IConfigurationValidator
{
    void ValidateConfiguration();
}

public class ConfigurationValidator(IOptionsMonitor<QueryPushSettings> options) : IConfigurationValidator
{
    public void ValidateConfiguration()
    {
        var settings = options.CurrentValue;
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(settings);

        ValidateObject(settings, validationContext, validationResults);

        foreach (var database in settings.Databases)
        {
            ValidateObject(database, new ValidationContext(database), validationResults);
            ValidateDatabaseConnection(database, validationResults);
        }

        foreach (var endpoint in settings.Endpoints)
        {
            ValidateObject(endpoint, new ValidationContext(endpoint), validationResults);
            foreach (var header in endpoint.Headers)
                ValidateObject(header, new ValidationContext(header), validationResults);
        }

        if (settings.Alerts.Slack != null)
            ValidateObject(settings.Alerts.Slack, new ValidationContext(settings.Alerts.Slack), validationResults);

        if (settings.Alerts.Email != null)
            ValidateObject(settings.Alerts.Email, new ValidationContext(settings.Alerts.Email), validationResults);

        foreach (var query in settings.Queries)
        {
            ValidateObject(query, new ValidationContext(query), validationResults);
            ValidateQueryReferences(query, settings, validationResults);
        }

        if (validationResults.Count > 0)
        {
            var errors = string.Join(Environment.NewLine, validationResults.Select(r => r.ErrorMessage));
            throw new ValidationException($"Configuration validation failed:{Environment.NewLine}{errors}");
        }
    }

    private static void ValidateObject(object obj, ValidationContext context, List<ValidationResult> results)
    {
        Validator.TryValidateObject(obj, context, results, validateAllProperties: true);
    }

    private static void ValidateDatabaseConnection(DatabaseConfig database, List<ValidationResult> results)
    {
        try
        {
            using var connection = new OdbcConnection(database.ConnectionString);
            connection.Open();
            connection.Close();
        }
        catch (Exception ex) when (ex.Message.Contains("driver", StringComparison.OrdinalIgnoreCase))
        {
            results.Add(new ValidationResult($"ODBC driver missing or invalid for database '{database.Name}': {ex.Message}"));
        }
        catch (Exception ex)
        {
            results.Add(new ValidationResult($"Cannot connect to database '{database.Name}': {ex.Message}"));
        }
    }

    private static void ValidateQueryReferences(QueryConfig query, QueryPushSettings settings, List<ValidationResult> results)
    {
        if (!settings.Databases.Any(d => d.Name == query.Database))
            results.Add(new ValidationResult($"Query '{query.Name}' references unknown database '{query.Database}'"));

        if (!settings.Endpoints.Any(e => e.Name == query.Endpoint))
            results.Add(new ValidationResult($"Query '{query.Name}' references unknown endpoint '{query.Endpoint}'"));

        if (query.OnFailure == FailureActionType.SlackAlert && settings.Alerts.Slack == null)
            results.Add(new ValidationResult($"Query '{query.Name}' uses SlackAlert but Slack configuration is missing"));

        if (query.OnFailure == FailureActionType.EmailAlert && settings.Alerts.Email == null)
            results.Add(new ValidationResult($"Query '{query.Name}' uses EmailAlert but Email configuration is missing"));

        if (string.IsNullOrEmpty(query.QueryText) && string.IsNullOrEmpty(query.QueryFile))
            results.Add(new ValidationResult($"Query '{query.Name}' must specify either QueryText or QueryFile"));

        if (!string.IsNullOrEmpty(query.QueryText) && !string.IsNullOrEmpty(query.QueryFile))
            results.Add(new ValidationResult($"Query '{query.Name}' cannot specify both QueryText and QueryFile"));

        if (!string.IsNullOrEmpty(query.QueryFile) && !File.Exists(query.QueryFile))
            results.Add(new ValidationResult($"Query '{query.Name}' QueryFile '{query.QueryFile}' not found"));
    }
}
