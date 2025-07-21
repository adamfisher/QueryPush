using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace QueryPush.Services;

public interface IVariableReplacer
{
    string Replace(string input, string queryName, IStateManager stateManager);
}

public class VariableReplacer(ILogger<VariableReplacer> logger) : IVariableReplacer
{
    private static readonly Regex VariableRegex = new(@"\{([^}]+)\}", RegexOptions.Compiled);
    private static readonly Regex OffsetFormatRegex = new(@"^(.+?)\|([+-]\d{2}:\d{2}:\d{2})\|(.+)$", RegexOptions.Compiled);
    private static readonly Regex FormatOnlyRegex = new(@"^(.+?)\|(.+)$", RegexOptions.Compiled);

    public string Replace(string input, string queryName, IStateManager stateManager)
    {
        var result = VariableRegex.Replace(input, match =>
        {
            var variable = match.Groups[1].Value;
            var replacement = ReplaceVariable(variable, queryName, stateManager);
            logger.LogDebug("Replaced variable '{{{Variable}}}' with '{Replacement}' for query '{QueryName}'", 
                variable, replacement, queryName);
            return replacement;
        });

        if (result != input)
        {
            logger.LogDebug("Variable replacement completed for query '{QueryName}' (found {VariableCount} variables)", 
                queryName, VariableRegex.Matches(input).Count);
        }

        return result;
    }

    private string ReplaceVariable(string variable, string queryName, IStateManager stateManager)
    {
        var offsetMatch = OffsetFormatRegex.Match(variable);
        if (offsetMatch.Success)
        {
            var baseVar = offsetMatch.Groups[1].Value;
            var offset = TimeSpan.Parse(offsetMatch.Groups[2].Value);
            var format = offsetMatch.Groups[3].Value;
            var baseValue = GetBaseVariableValue(baseVar, queryName, stateManager);
            
            if (baseValue is DateTime dateTime)
                return dateTime.Add(offset).ToString(format);
        }

        var formatMatch = FormatOnlyRegex.Match(variable);
        if (formatMatch.Success)
        {
            var baseVar = formatMatch.Groups[1].Value;
            var format = formatMatch.Groups[2].Value;
            var baseValue = GetBaseVariableValue(baseVar, queryName, stateManager);
            
            if (baseValue is DateTime dateTime)
                return dateTime.ToString(format);
        }

        return GetBaseVariableValue(variable, queryName, stateManager)?.ToString() ?? string.Empty;
    }

    private object? GetBaseVariableValue(string variable, string queryName, IStateManager stateManager)
    {
        return variable switch
        {
            "DateTimeNow" => DateTime.Now,
            "UtcNow" => DateTime.UtcNow,
            "DateNow" => DateTime.Now.Date,
            "LastRun" => stateManager.GetLastRun(queryName),
            "Guid" => Guid.NewGuid(),
            "MachineName" => Environment.MachineName,
            var env when env.StartsWith("Env:") => Environment.GetEnvironmentVariable(env[4..]),
            _ => null
        };
    }
}
