using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QueryPush.Configuration;
using QueryPush.Services;
using Xunit;
using Xunit.Categories;

namespace QueryPush.Tests;

public class DatabaseIntegrationTests : IDisposable
{
    private readonly IServiceProvider _services;
    private readonly string _testDbPath = "integration_test.db";

    public DatabaseIntegrationTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["databases:0:name"] = "TestDb",
                ["databases:0:connectionString"] = $"Driver={{SQLite3 ODBC Driver}};Database={_testDbPath};",
                ["endpoints:0:name"] = "TestEndpoint",
                ["endpoints:0:url"] = "https://webhook.site/test"
            })
            .Build();

        var services = new ServiceCollection();
        services.Configure<QueryPushSettings>(configuration);
        services.AddLogging(builder => builder.ClearProviders());
        services.AddScoped<IDatabaseService, DatabaseService>();

        _services = services.BuildServiceProvider();
    }

    [Fact, IntegrationTest]
    public async Task DatabaseService_WithSQLiteODBC_ShouldCreateTableAndQuery()
    {
        var dbService = _services.GetRequiredService<IDatabaseService>();

        await dbService.ExecuteQueryAsync("TestDb", 
            "CREATE TABLE IF NOT EXISTS users (id INTEGER PRIMARY KEY, name TEXT, email TEXT)", 30, 1000);

        await dbService.ExecuteQueryAsync("TestDb", 
            "INSERT OR REPLACE INTO users (id, name, email) VALUES (1, 'John Doe', 'john@example.com'), (2, 'Jane Smith', 'jane@example.com')", 30, 1000);

        var results = await dbService.ExecuteQueryAsync("TestDb", 
            "SELECT id, name, email FROM users ORDER BY id", 30, 1000);

        results.Should().HaveCount(2);
        results.First()["name"].Should().Be("John Doe");
        results.Last()["email"].Should().Be("jane@example.com");
    }

    [Fact, IntegrationTest]
    public async Task DatabaseService_WithInvalidQuery_ShouldThrow()
    {
        var dbService = _services.GetRequiredService<IDatabaseService>();

        var act = async () => await dbService.ExecuteQueryAsync("TestDb", "SELECT * FROM nonexistent_table", 30, 100);

        await act.Should().ThrowAsync<Exception>();
    }

    public void Dispose()
    {
        if (File.Exists(_testDbPath))
            File.Delete(_testDbPath);
    }
}
