namespace PoesieDuLundi.SharedKernel;

public abstract class Entity : IEquatable<Entity>
{
    public Guid Id { get; }

    protected Entity()
    {
        // Version 7 GUIDs are time-ordered, which keeps clustered index writes local instead of
        // scattering inserts across a Postgres primary-key btree.
        Id = Guid.CreateVersion7();
    }

    protected Entity(Guid id)
    {
        Id = id;
    }

    public bool Equals(Entity? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return Id == other.Id;
    }

    public override bool Equals(object? obj) => Equals(obj as Entity);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity? left, Entity? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Entity? left, Entity? right) => !(left == right);
}
