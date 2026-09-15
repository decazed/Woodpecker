namespace Woodpecker.Domain;

public class User : Entity
{
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public UserRole Role { get; private set; }

    private User(Guid id, string email, string passwordHash, UserRole role) : base(id)
    {
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
    }

    // PasswordHash arrive déjà hashé : le domaine ne connaît pas l'algorithme de hashing
    // (c'est une responsabilité d'Infrastructure, cf. IPasswordHasher côté Application).
    public static User Create(string email, string passwordHash, UserRole role = UserRole.Member)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("L'email ne peut pas être vide.", nameof(email));

        if (!email.Contains('@'))
            throw new ArgumentException("L'email doit contenir un '@'.", nameof(email));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Le mot de passe ne peut pas être vide.", nameof(passwordHash));

        return new User(Guid.NewGuid(), email, passwordHash, role);
    }
}
