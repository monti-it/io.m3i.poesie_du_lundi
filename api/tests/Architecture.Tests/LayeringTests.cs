using System.Reflection;
using NetArchTest.Rules;

namespace PoesieDuLundi.Architecture.Tests;

/// <summary>
/// This app is a single project layered by namespace, not by <c>.csproj</c> (deviation from the
/// reference modular-monolith layout — docs/ENGINEERING_PRACTICES.md "This app's layout"), so the
/// layer-direction rule has no compiler-enforced project graph to lean on. These tests are that
/// boundary instead: a stray <c>using</c>, a use case reaching for <c>HttpContext</c>, a
/// <c>Domain</c> type pulling in EF Core.
/// </summary>
public class LayeringTests
{
    private static readonly Assembly App = typeof(PoesieDuLundi.SharedKernel.Result).Assembly;

    // Infrastructure/frameworks pure business/model code must never touch.
    private static readonly string[] ForbiddenInDomain =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        "Npgsql",
    ];

    private static PredicateList InNamespace(string @namespace) =>
        Types.InAssembly(App).That().ResideInNamespaceStartingWith($"PoesieDuLundi.{@namespace}");

    private static void AssertNoDependency(PredicateList types, params string[] forbidden)
    {
        var result = types.Should().NotHaveDependencyOnAny(forbidden).GetResult();

        Assert.True(
            result.IsSuccessful,
            $"Violations: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void SharedKernel_depends_only_on_the_bcl()
    {
        AssertNoDependency(InNamespace("SharedKernel"), ForbiddenInDomain);
    }

    [Fact]
    public void Domain_depends_only_on_the_bcl_and_SharedKernel()
    {
        AssertNoDependency(InNamespace("Domain"), ForbiddenInDomain);
    }

    [Fact]
    public void Application_is_web_free_and_ef_free()
    {
        AssertNoDependency(InNamespace("Application"), ForbiddenInDomain);
    }

    [Fact]
    public void Api_handlers_hold_no_ef_core_types()
    {
        AssertNoDependency(InNamespace("Api"), "Microsoft.EntityFrameworkCore", "Npgsql");
    }

    [Fact]
    public void Public_api_does_not_reference_admin_api()
    {
        AssertNoDependency(InNamespace("Api.Public"), "PoesieDuLundi.Api.Admin");
    }

    [Fact]
    public void Admin_api_does_not_reference_public_api()
    {
        AssertNoDependency(InNamespace("Api.Admin"), "PoesieDuLundi.Api.Public");
    }
}
