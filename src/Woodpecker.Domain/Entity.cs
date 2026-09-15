namespace Woodpecker.Domain;

public abstract class Entity
{
    public Guid Id { get; }

    protected Entity(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("L'identifiant ne peut pas être vide.", nameof(id));

        Id = id;
    }

    // Deux entités sont égales si elles ont le même type et le même Id,
    // peu importe l'état de leurs autres propriétés.
    public override bool Equals(object? obj)
    {
        if (obj is not Entity other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;

        return Id == other.Id;
    }

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity? left, Entity? right) => Equals(left, right);
    public static bool operator !=(Entity? left, Entity? right) => !Equals(left, right);
}
