using System.Linq.Expressions;
using PoesieDuLundi.Domain;

namespace PoesieDuLundi.Infrastructure.Public;

/// <summary>The "effectively published" predicate (<see cref="Poem.IsEffectivelyPublished"/>, which
/// isn't itself SQL-translatable) as a reusable, query-translatable expression — every public read
/// query filters on it.</summary>
internal static class PublishedPoems
{
    public static Expression<Func<Poem, bool>> AsOf(DateOnly today) => poem =>
        poem.Status == PoemStatus.Published
        || (poem.Status == PoemStatus.Scheduled && poem.PublicationDate <= today);
}
