using Microsoft.AspNetCore.Identity;
using Woodpecker.Application.Abstractions;

namespace Woodpecker.Infrastructure.Auth;

// S'appuie sur l'algorithme de hashing d'ASP.NET Core Identity (PBKDF2, salé,
// nombre d'itérations configurable) plutôt que d'écrire notre propre hashing —
// jamais une bonne idée de réinventer ça soi-même.
public class PasswordHasher : IPasswordHasher
{
    private readonly Microsoft.AspNetCore.Identity.PasswordHasher<object> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(new object(), password);

    public bool Verify(string password, string passwordHash) =>
        _hasher.VerifyHashedPassword(new object(), passwordHash, password) != PasswordVerificationResult.Failed;
}
