using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Woodpecker.Api.Contracts;

namespace Woodpecker.Api.Tests;

[Collection("Api")]
public class AuthTests
{
    private readonly HttpClient _client;

    public AuthTests(WoodpeckerApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ThenLogin_ReturnsToken()
    {
        var email = $"{Guid.NewGuid()}@example.com";

        var registerResponse = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "SuperSecret123"));
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, "SuperSecret123"));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        body!.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "SuperSecret123"));

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "WrongPassword"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_WithAlreadyUsedEmail_ReturnsConflict()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "SuperSecret123"));

        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "AnotherPass123"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/puzzle-sets",
            new CreatePuzzleSetRequest("Set", new[] { Guid.NewGuid() }));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
