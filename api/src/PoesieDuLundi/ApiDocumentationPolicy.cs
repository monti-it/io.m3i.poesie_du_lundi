namespace PoesieDuLundi;

/// <summary>
/// Decides whether the OpenAPI document and Swagger UI at <c>/api/admin/swagger</c> (see
/// <see cref="Api.Admin.AdminApiDocs"/>) are mounted at all this run. The forwardAuth gate on
/// <c>/api/admin</c> already keeps docs off the anonymous internet when mounted — this is an
/// extra kill switch, not the auth check itself: off in <c>Testing</c> (nothing to gate against
/// in the test host) and toggleable everywhere else via <c>ApiDocs__Enabled</c> without a deploy.
/// </summary>
public static class ApiDocumentationPolicy
{
    public static bool ShouldExposeDocs(IHostEnvironment environment, IConfiguration configuration)
    {
        if (environment.IsEnvironment("Testing"))
        {
            return false;
        }

        return configuration.GetValue("ApiDocs:Enabled", true);
    }
}
