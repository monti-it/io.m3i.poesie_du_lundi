using Microsoft.EntityFrameworkCore;
using PoesieDuLundi.Application;
using PoesieDuLundi.Domain;
using PoesieDuLundi.Infrastructure.Public;
using PoesieDuLundi.SharedKernel;

namespace PoesieDuLundi.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddPoesieDuLundiInfrastructure(
        this IServiceCollection services, DatabaseOptions database, PublicationJobOptions publicationJob)
    {
        services.AddScoped<DomainEventDispatcher>();
        services.AddScoped<DomainEventDispatchInterceptor>();

        services.AddDbContext<PoesieDuLundiDbContext>((serviceProvider, options) =>
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

            options.AddInterceptors(serviceProvider.GetRequiredService<DomainEventDispatchInterceptor>());
        });

        services.AddScoped<IPoemRepository, PoemRepository>();
        services.AddScoped<ListPoemsQuery>();
        services.AddScoped<ListSeriesQuery>();
        services.AddScoped<CreateDraftPoem>();
        services.AddScoped<UpdatePoem>();
        services.AddScoped<DeletePoem>();
        services.AddScoped<SchedulePoemForMonday>();
        services.AddScoped<PublishPoem>();
        services.AddScoped<UnpublishPoem>();
        services.AddScoped<MaterialiseDuePoems>();
        services.AddScoped<ListPublishedPoemsQuery>();
        services.AddScoped<GetPublishedPoemBySlugQuery>();
        services.AddScoped<GetThisMondayPoemQuery>();
        services.AddScoped<GetArchiveQuery>();
        services.AddScoped<GetSeriesBySlugQuery>();
        services.AddScoped<ListTagsQuery>();
        services.AddScoped<ListFeedPoemsQuery>();
        services.AddScoped<ListSitemapPoemsQuery>();
        services.AddScoped<IDomainEventHandler<PoemPublished>, FeedCacheInvalidationHandler>();
        services.AddScoped<IDomainEventHandler<PoemUnpublished>, FeedCacheInvalidationHandler>();
        services.AddSingleton(TimeProvider.System);

        services.AddSingleton(publicationJob);
        services.AddHostedService<PoemPublicationBackgroundService>();

        return services;
    }
}
