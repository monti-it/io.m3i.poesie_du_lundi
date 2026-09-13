using PoesieDuLundi.SharedKernel;

namespace PoesieDuLundi.Tests.SharedKernel;

public class EntityTests
{
    private sealed class TestEntity : Entity
    {
        public TestEntity()
        {
        }

        public TestEntity(Guid id) : base(id)
        {
        }
    }

    private sealed class OtherEntity(Guid id) : Entity(id);

    [Fact]
    public void New_entities_get_distinct_ids()
    {
        var first = new TestEntity();
        var second = new TestEntity();

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void Entities_with_the_same_id_and_type_are_equal()
    {
        var id = Guid.CreateVersion7();

        var first = new TestEntity(id);
        var second = new TestEntity(id);

        Assert.Equal(first, second);
        Assert.True(first == second);
    }

    [Fact]
    public void Entities_of_different_types_with_the_same_id_are_not_equal()
    {
        var id = Guid.CreateVersion7();

        Entity first = new TestEntity(id);
        Entity second = new OtherEntity(id);

        Assert.False(first.Equals(second));
    }

    [Fact]
    public void Entities_with_different_ids_are_not_equal()
    {
        var first = new TestEntity(Guid.CreateVersion7());
        var second = new TestEntity(Guid.CreateVersion7());

        Assert.NotEqual(first, second);
        Assert.True(first != second);
    }
}
