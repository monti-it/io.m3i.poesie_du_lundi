using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using PoesieDuLundi;
using PoesieDuLundi.Infrastructure;

namespace PoesieDuLundi.Tests;

public class PublicationJobStartupPolicyTests
{
    [Fact]
    public void Does_not_run_in_Testing()
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns("Testing");

        Assert.False(PublicationJobStartupPolicy.ShouldRun(environment));
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public void Runs_outside_Testing(string environmentName)
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);

        Assert.True(PublicationJobStartupPolicy.ShouldRun(environment));
    }

    [Fact]
    public void Defaults_the_interval_when_unconfigured()
    {
        var configuration = new ConfigurationBuilder().Build();

        Assert.Equal(PublicationJobOptions.DefaultInterval, PublicationJobStartupPolicy.GetInterval(configuration));
    }

    [Fact]
    public void The_interval_is_configurable()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Publishing:MaterialisationInterval"] = "00:10:00",
            })
            .Build();

        Assert.Equal(TimeSpan.FromMinutes(10), PublicationJobStartupPolicy.GetInterval(configuration));
    }
}
