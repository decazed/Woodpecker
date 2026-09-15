namespace Woodpecker.Api.Contracts;

public record RegisterRequest(string Email, string Password);

public record RegisterResponse(Guid Id);

public record LoginRequest(string Email, string Password);

public record LoginResponse(string Token);
