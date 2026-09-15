using Microsoft.EntityFrameworkCore;
using PoesieDuLundi.Domain;
using PoesieDuLundi.Infrastructure;

namespace PoesieDuLundi.Tests.Infrastructure;

public class ListSeriesQueryTests
{
    private static PoesieDuLundiDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<PoesieDuLundiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task Lists_every_series_ordered_by_its_Order()
    {
        await using var dbContext = CreateDbContext();
        var second = new Series("Seconde saison", 2);
        var first = new Series("Première saison", 1);
        await dbContext.Series.AddRangeAsync(second, first);
        await dbContext.SaveChangesAsync();
        var query = new ListSeriesQuery(dbContext);

        var result = (await query.HandleAsync(CancellationToken.None)).ToList();

        Assert.Equal([first.Id, second.Id], result.Select(s => s.Id));
    }
}
