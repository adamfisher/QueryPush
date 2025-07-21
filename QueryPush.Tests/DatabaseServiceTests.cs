using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QueryPush.Configuration;
using QueryPush.Services;
using Xunit;
using Xunit.Categories;

namespace QueryPush.Tests;

public class DatabaseServiceTests : IDisposable
{
    private readonly IServiceProvider _services;
    private readonly string _testDbPath = "database_test.db";

    public DatabaseServiceTests()
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
    public async Task ExecuteQueryAsync_WithValidSqliteQuery_ShouldReturnResults()
    {
        var service = _services.GetRequiredService<IDatabaseService>();

        await service.ExecuteQueryAsync("TestDb", "CREATE TABLE IF NOT EXISTS temp_test (id INTEGER, name TEXT)", 30, 100);
        await service.ExecuteQueryAsync("TestDb", "INSERT OR REPLACE INTO temp_test VALUES (1, 'Test')", 30, 100);
        
        var results = await service.ExecuteQueryAsync("TestDb", "SELECT * FROM temp_test", 30, 100);

        results.Should().HaveCount(1);
        results.First()["id"].Should().Be(1);
        results.First()["name"].Should().Be("Test");
    }

    [Fact, IntegrationTest]
    public async Task ExecuteQueryAsync_WithInvalidDatabase_ShouldThrow()
    {
        var service = _services.GetRequiredService<IDatabaseService>();

        var act = async () => await service.ExecuteQueryAsync("NonExistent", "SELECT 1", 30, 100);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    public void Dispose()
    {
        if (File.Exists(_testDbPath))
            File.Delete(_testDbPath);
    }
}
