using PoesieDuLundi.SharedKernel;

namespace PoesieDuLundi.Tests.SharedKernel;

public class DateRangeTests
{
    [Fact]
    public void End_before_start_is_rejected()
    {
        var start = new DateOnly(2026, 1, 10);
        var end = new DateOnly(2026, 1, 1);

        Assert.Throws<ArgumentException>(() => new DateRange(start, end));
    }

    [Fact]
    public void Contains_is_inclusive_of_both_bounds()
    {
        var range = new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        Assert.True(range.Contains(new DateOnly(2026, 1, 1)));
        Assert.True(range.Contains(new DateOnly(2026, 1, 31)));
        Assert.True(range.Contains(new DateOnly(2026, 1, 15)));
        Assert.False(range.Contains(new DateOnly(2025, 12, 31)));
        Assert.False(range.Contains(new DateOnly(2026, 2, 1)));
    }

    [Fact]
    public void Overlaps_detects_shared_days()
    {
        var january = new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));
        var lateJanuary = new DateRange(new DateOnly(2026, 1, 20), new DateOnly(2026, 2, 5));
        var march = new DateRange(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31));

        Assert.True(january.Overlaps(lateJanuary));
        Assert.False(january.Overlaps(march));
    }

    [Fact]
    public void Ranges_with_the_same_bounds_are_equal()
    {
        var first = new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));
        var second = new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        Assert.Equal(first, second);
    }
}
