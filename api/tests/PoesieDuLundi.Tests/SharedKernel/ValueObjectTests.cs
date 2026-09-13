using PoesieDuLundi.SharedKernel;

namespace PoesieDuLundi.Tests.SharedKernel;

public class ValueObjectTests
{
    private sealed class Point(int x, int y) : ValueObject
    {
        public int X { get; } = x;
        public int Y { get; } = y;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return X;
            yield return Y;
        }
    }

    private sealed class OtherPoint(int x, int y) : ValueObject
    {
        public int X { get; } = x;
        public int Y { get; } = y;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return X;
            yield return Y;
        }
    }

    [Fact]
    public void Value_objects_with_the_same_components_are_equal()
    {
        Assert.Equal(new Point(1, 2), new Point(1, 2));
        Assert.True(new Point(1, 2) == new Point(1, 2));
        Assert.Equal(new Point(1, 2).GetHashCode(), new Point(1, 2).GetHashCode());
    }

    [Fact]
    public void Value_objects_with_different_components_are_not_equal()
    {
        Assert.NotEqual(new Point(1, 2), new Point(1, 3));
        Assert.True(new Point(1, 2) != new Point(1, 3));
    }

    [Fact]
    public void Value_objects_of_different_types_are_not_equal_even_with_the_same_components()
    {
        ValueObject point = new Point(1, 2);
        ValueObject otherPoint = new OtherPoint(1, 2);

        Assert.False(point.Equals(otherPoint));
    }
}
