using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace QueryPush.Services;

public interface IStateManager
{
    DateTime? GetLastRun(string queryName);
    void SetLastRun(string queryName, DateTime timestamp);
    DateTime? GetLastAlert(string queryName, string alertType);
    void SetLastAlert(string queryName, string alertType, DateTime timestamp);
    Task SaveAsync();
    Task LoadAsync();
}

public class StateManager(ILogger<StateManager> logger) : IStateManager
{
    private readonly string _stateFilePath = GetStateFilePath();
    private readonly SemaphoreSlim _lock = new(1, 1);
    private QueryState _state = new();

    private static string GetStateFilePath()
    {
        var currentDirectory = AppContext.BaseDirectory;
        return Path.Combine(currentDirectory, "QueryState.json");
    }

    public DateTime? GetLastRun(string queryName)
    {
        return _state.LastRunTimes.TryGetValue(queryName, out var lastRun) ? lastRun : null;
    }

    public void SetLastRun(string queryName, DateTime timestamp)
    {
        _state.LastRunTimes[queryName] = timestamp;
        logger.LogDebug("Set last run for '{QueryName}' to {Timestamp}", queryName, timestamp);
    }

    public DateTime? GetLastAlert(string queryName, string alertType)
    {
        var key = $"{queryName}:{alertType}";
        return _state.LastAlertTimes.TryGetValue(key, out var lastAlert) ? lastAlert : null;
    }

    public void SetLastAlert(string queryName, string alertType, DateTime timestamp)
    {
        var key = $"{queryName}:{alertType}";
        _state.LastAlertTimes[key] = timestamp;
        logger.LogDebug("Set last {AlertType} alert for '{QueryName}' to {Timestamp}", alertType, queryName, timestamp);
    }

    public async Task LoadAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (File.Exists(_stateFilePath))
            {
                var json = await File.ReadAllTextAsync(_stateFilePath);
                _state = JsonSerializer.Deserialize<QueryState>(json) ?? new QueryState();
                logger.LogInformation("Loaded state with {QueryCount} query states and {AlertCount} alert states", 
                    _state.LastRunTimes.Count, _state.LastAlertTimes.Count);
            }
            else
            {
                logger.LogInformation("No existing state file found, starting with empty state");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load state from {StateFilePath}", _stateFilePath);
            _state = new QueryState();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var tempPath = _stateFilePath + ".tmp";
            var json = JsonSerializer.Serialize(_state, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(tempPath, json);
            
            if (File.Exists(_stateFilePath))
                File.Delete(_stateFilePath);
            
            File.Move(tempPath, _stateFilePath);
            logger.LogDebug("Saved state to {StateFilePath}", _stateFilePath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save state to {StateFilePath}", _stateFilePath);
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }
}

public class QueryState
{
    public Dictionary<string, DateTime> LastRunTimes { get; set; } = new();
    public Dictionary<string, DateTime> LastAlertTimes { get; set; } = new();
}
