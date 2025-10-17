using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using QueryPush.Configuration;
using QueryPush.Services;
using Serilog;

namespace QueryPush;

public class Program
{
    private static async Task Main(string[] args)
    {
        var isService = args.Contains("--service");
        var builder = CreateHostBuilder(args, isService);
        
        var host = builder.Build();
        
        var validator = host.Services.GetRequiredService<IConfigurationValidator>();
        validator.ValidateConfiguration();
        
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("QueryPush starting in {Mode} mode", (isService ? "service" : "console"));
        
        await host.RunAsync();
    }

    private static HostApplicationBuilder CreateHostBuilder(string[] args, bool isService)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Configuration
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args);

        builder.Services.Configure<QueryPushSettings>(
            builder.Configuration);

        ConfigureLogging(builder);
        RegisterServices(builder.Services);
        ConfigurePlatformSpecific(builder, isService);

        return builder;
    }

    private static void RegisterServices(IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddSingleton<IDatabaseConnectionFactory, DatabaseConnectionFactory>();
        services.AddSingleton<IConfigurationValidator, ConfigurationValidator>();
        services.AddSingleton<IStateManager, StateManager>();
        services.AddSingleton<IVariableReplacer, VariableReplacer>();
        services.AddSingleton<IQueryTextResolver, QueryTextResolver>();
        services.AddScoped<IDatabaseService, DatabaseService>();
        services.AddScoped<IHttpService, HttpService>();
        services.AddScoped<IAlertService, AlertService>();
        services.AddScoped<IQueryScheduler, QueryScheduler>();
        services.AddScoped<IQueryExecutor, QueryExecutor>();

        services.AddQuartz();
        services.AddQuartzHostedService(opt => opt.WaitForJobsToComplete = true);
        services.AddScoped<QueryJob>();
        services.AddHostedService<QuartzSchedulerService>();
    }

    private static void ConfigurePlatformSpecific(HostApplicationBuilder builder, bool isService)
    {
        if (OperatingSystem.IsWindows() && isService)
        {
            builder.Services.AddWindowsService(options =>
            {
                options.ServiceName = nameof(QueryPush);
            });
        }
        else if (OperatingSystem.IsLinux() && isService)
        {
            builder.Services.AddSystemd();
        }
    }

    private static void ConfigureLogging(HostApplicationBuilder builder)
    {
        builder.Services.AddSerilog((services, config) =>
        {
            var settings = services.GetService<IOptions<QueryPushSettings>>()?.Value?.Logging ?? new LoggingConfig();
            var rotationInterval = settings.RotationStrategy switch
            {
                LogRotationStrategy.Daily => RollingInterval.Day,
                LogRotationStrategy.Weekly => RollingInterval.Day,
                LogRotationStrategy.Monthly => RollingInterval.Month,
                _ => RollingInterval.Infinite
            };

            config.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
                  .WriteTo.File(
                      path: Path.Combine(settings.LogDirectory, "querypush-.log"),
                      rollingInterval: rotationInterval,
                      retainedFileCountLimit: rotationInterval == RollingInterval.Infinite ? null : settings.RetentionDays,
                      shared: true,
                      outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");

            if (OperatingSystem.IsWindows())
            {
                config.WriteTo.EventLog(source: nameof(QueryPush), logName: "Application");
            }
        });
    }
}
