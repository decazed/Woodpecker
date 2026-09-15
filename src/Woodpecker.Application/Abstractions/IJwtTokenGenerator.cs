using Woodpecker.Domain;

namespace Woodpecker.Application.Abstractions;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
