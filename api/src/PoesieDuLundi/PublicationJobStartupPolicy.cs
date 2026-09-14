using PoesieDuLundi.Infrastructure;

namespace PoesieDuLundi;

/// <summary>
/// Decides whether the publication-materialising background job (issue #17,
/// docs/ARCHITECTURE.md "Publishing model") actually sweeps this run: off in <c>Testing</c> — a
/// <see cref="Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory{TEntryPoint}"/> host doesn't
/// want a background timer racing test assertions against the InMemory provider — on everywhere
/// else. The sweep interval is separately configurable via <c>Publishing:MaterialisationInterval</c>
/// regardless of this switch.
/// </summary>
public static class PublicationJobStartupPolicy
{
    public static bool ShouldRun(IHostEnvironment environment) => !environment.IsEnvironment("Testing");

    public static TimeSpan GetInterval(IConfiguration configuration) =>
        configuration.GetValue("Publishing:MaterialisationInterval", PublicationJobOptions.DefaultInterval);
}
