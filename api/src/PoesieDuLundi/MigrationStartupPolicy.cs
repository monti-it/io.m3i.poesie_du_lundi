namespace PoesieDuLundi;

public static class MigrationStartupPolicy
{
    public static bool ShouldRunMigrationsOnStartup(IHostEnvironment environment, IConfiguration configuration)
    {
        if (environment.IsEnvironment("Testing"))
        {
            return false;
        }

        if (environment.IsDevelopment())
        {
            return true;
        }

        return configuration.GetValue<bool>("RunMigrationsOnStartup");
    }
}
