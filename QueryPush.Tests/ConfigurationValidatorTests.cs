using FluentAssertions;
using QueryPush.Configuration;
using QueryPush.Services;
using Xunit;
using Xunit.Categories;

namespace QueryPush.Tests;

public class ConfigurationValidatorTests
{
    [Fact, UnitTest]
    public void ValidateConfiguration_WithMissingDatabase_ShouldThrow()
    {
        var settings = new QueryPushSettings
        {
            Databases = [],
            Endpoints = [new EndpointConfig { Name = "test", Url = "http://test" }],
            Queries = []
        };
        
        var monitor = new TestOptionsMonitor<QueryPushSettings>(settings);
        var validator = new ConfigurationValidator(monitor);
        
        var act = () => validator.ValidateConfiguration();
        
        act.Should().Throw<System.ComponentModel.DataAnnotations.ValidationException>();
    }
}
