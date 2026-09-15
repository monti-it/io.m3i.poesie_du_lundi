using Microsoft.EntityFrameworkCore;
using PoesieDuLundi.Application;

namespace PoesieDuLundi.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddPoesieDuLundiInfrastructure(
        this IServiceCollection services, DatabaseOptions database, PublicationJobOptions publicationJob)
    {
        services.AddDbContext<PoesieDuLundiDbContext>(options =>
        {
            if (database.UsesInMemoryProvider)
            {
                options.UseInMemoryDatabase(
                    $"PoesieDuLundi-{database.InMemoryDatabaseName ?? "poesie"}");
            }
            else
            {
                options.UseNpgsql(database.ConnectionString, npgsql =>
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory"));
            }
        });

        services.AddScoped<IPoemRepository, PoemRepository>();
        services.AddScoped<ListPoemsQuery>();
        services.AddScoped<CreateDraftPoem>();
        services.AddScoped<UpdatePoem>();
        services.AddScoped<DeletePoem>();
        services.AddScoped<SchedulePoemForMonday>();
        services.AddScoped<PublishPoem>();
        services.AddScoped<UnpublishPoem>();
        services.AddScoped<MaterialiseDuePoems>();
        services.AddSingleton(TimeProvider.System);

        services.AddSingleton(publicationJob);
        services.AddHostedService<PoemPublicationBackgroundService>();

        return services;
    }
}
