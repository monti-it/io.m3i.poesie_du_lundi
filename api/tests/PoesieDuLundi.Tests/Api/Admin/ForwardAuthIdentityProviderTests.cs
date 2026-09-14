using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using PoesieDuLundi.Api.Admin;

namespace PoesieDuLundi.Tests.Api.Admin;

public class ForwardAuthIdentityProviderTests
{
    [Fact]
    public void Resolves_the_subject_from_the_Remote_User_header()
    {
        var provider = CreateProvider("Production", remoteUser: "alice@example.com");

        Assert.Equal("alice@example.com", provider.GetCurrentSubject());
    }

    [Fact]
    public void Returns_null_outside_Development_when_the_header_is_missing()
    {
        var provider = CreateProvider("Production", remoteUser: null);

        Assert.Null(provider.GetCurrentSubject());
    }

    [Fact]
    public void Falls_back_to_a_fixed_dev_subject_in_Development_when_the_header_is_missing()
    {
        var provider = CreateProvider("Development", remoteUser: null);

        Assert.Equal("dev@localhost", provider.GetCurrentSubject());
    }

    [Fact]
    public void The_dev_fallback_subject_is_configurable()
    {
        var provider = CreateProvider("Development", remoteUser: null, devSubject: "someone@else");

        Assert.Equal("someone@else", provider.GetCurrentSubject());
    }

    [Fact]
    public void The_header_wins_over_the_dev_fallback_in_Development()
    {
        var provider = CreateProvider("Development", remoteUser: "alice", devSubject: "dev@localhost");

        Assert.Equal("alice", provider.GetCurrentSubject());
    }

    private static ForwardAuthIdentityProvider CreateProvider(
        string environmentName, string? remoteUser, string? devSubject = null)
    {
        var httpContext = new DefaultHttpContext();
        if (remoteUser is not null)
        {
            httpContext.Request.Headers[ForwardAuthIdentityProvider.UserHeader] = remoteUser;
        }

        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);

        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Auth:DevSubject"] = devSubject })
            .Build();

        return new ForwardAuthIdentityProvider(accessor, environment, configuration);
    }
}
