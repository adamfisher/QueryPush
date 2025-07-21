using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using QueryPush.Configuration;

namespace QueryPush.Services;

public class QuartzSchedulerService(
    ISchedulerFactory schedulerFactory,
    IOptionsMonitor<QueryPushSettings> options,
    IStateManager stateManager,
    ILogger<QuartzSchedulerService> logger) : IHostedService
{
    private IScheduler? _scheduler;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await stateManager.LoadAsync();
        
        _scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        await _scheduler.Start(cancellationToken);
        
        logger.LogInformation("Quartz scheduler started");
        await ScheduleQueriesAsync();
        
        options.OnChange(_ => Task.Run(RescheduleQueriesAsync));
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_scheduler != null)
        {
            await _scheduler.Shutdown(cancellationToken);
            logger.LogInformation("Quartz scheduler stopped");
        }
    }

    private async Task ScheduleQueriesAsync()
    {
        var settings = options.CurrentValue;
        var scheduledCount = 0;
        
        foreach (var query in settings.Queries.Where(q => q.Enabled))
        {
            await ScheduleQueryAsync(query);
            scheduledCount++;
            
            if (query.RunOnStartup)
            {
                await ScheduleStartupJobAsync(query);
                logger.LogInformation("Scheduled startup execution for query '{QueryName}'", query.Name);
            }
        }
        
        logger.LogInformation("Scheduled {QueryCount} queries", scheduledCount);
    }

    private async Task RescheduleQueriesAsync()
    {
        logger.LogInformation("Configuration changed, rescheduling queries");
        
        if (_scheduler != null)
        {
            await _scheduler.Clear();
            await ScheduleQueriesAsync();
        }
    }

    private async Task ScheduleQueryAsync(QueryConfig query)
    {
        var job = JobBuilder.Create<QueryJob>()
            .WithIdentity($"query-{query.Name}")
            .UsingJobData("queryName", query.Name)
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity($"trigger-{query.Name}")
            .WithCronSchedule(query.Cron)
            .Build();

        await _scheduler!.ScheduleJob(job, trigger);
        
        logger.LogDebug("Scheduled query '{QueryName}' with cron '{CronExpression}'", query.Name, query.Cron);
    }

    private async Task ScheduleStartupJobAsync(QueryConfig query)
    {
        var job = JobBuilder.Create<QueryJob>()
            .WithIdentity($"startup-{query.Name}")
            .UsingJobData("queryName", query.Name)
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity($"startup-trigger-{query.Name}")
            .StartNow()
            .Build();

        await _scheduler!.ScheduleJob(job, trigger);
    }
}
