using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Woodpecker.Api.Contracts;
using Woodpecker.Application.Abstractions;
using Woodpecker.Application.Statistics;
using Woodpecker.Domain;
using Woodpecker.Infrastructure;

namespace Woodpecker.Api.Tests;

// Test de bout en bout du parcours complet : un admin importe des puzzles, un membre
// crée un set, démarre un cycle, résout un puzzle, puis consulte ses statistiques.
// Exercice tout le pipeline HTTP réel (routing, JWT, validation, MediatR, EF Core).
[Collection("Api")]
public class TrainingFlowTests
{
    private readonly WoodpeckerApiFactory _factory;
    private readonly HttpClient _client;

    public TrainingFlowTests(WoodpeckerApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task SeedAdminAsync(string email, string password)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WoodpeckerDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        context.Users.Add(User.Create(email, hasher.Hash(password), UserRole.Admin));
        await context.SaveChangesAsync();
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.Token;
    }

    [Fact]
    public async Task FullTrainingFlow_FromImportToStats_Succeeds()
    {
        var adminEmail = $"{Guid.NewGuid()}@example.com";
        await SeedAdminAsync(adminEmail, "AdminPass123");
        var adminToken = await LoginAsync(adminEmail, "AdminPass123");

        var memberEmail = $"{Guid.NewGuid()}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(memberEmail, "MemberPass123"));
        var memberToken = await LoginAsync(memberEmail, "MemberPass123");

        // 1. L'admin importe un puzzle : position après 1.e4 (trait aux Noirs), ...e5 est
        // auto-jouée (indice 0), le membre (Blancs) doit trouver Cf3 (indice 1).
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var importResponse = await _client.PostAsJsonAsync("/api/puzzles/import", new ImportPuzzlesRequest(
            new[]
            {
                new ImportPuzzleItemRequest(
                    "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1",
                    new[] { "e7e5", "g1f3" },
                    1200),
            }));
        importResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var puzzleId = (await importResponse.Content.ReadFromJsonAsync<ImportPuzzlesResponse>())!.Ids[0];

        // 2. Un membre ne peut pas importer (réservé aux admins)
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
        var forbiddenImport = await _client.PostAsJsonAsync("/api/puzzles/import", new ImportPuzzlesRequest(
            new[]
            {
                new ImportPuzzleItemRequest(
                    "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
                    new[] { "d2d4", "d7d5" },
                    1200),
            }));
        forbiddenImport.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 3. Le membre crée un set avec le puzzle importé
        var createSetResponse = await _client.PostAsJsonAsync(
            "/api/puzzle-sets",
            new CreatePuzzleSetRequest("Mon set", new[] { puzzleId }));
        createSetResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var setId = (await createSetResponse.Content.ReadFromJsonAsync<CreatePuzzleSetResponse>())!.Id;

        // 4. Il démarre un cycle
        var startCycleResponse = await _client.PostAsJsonAsync(
            "/api/training-cycles",
            new StartTrainingCycleRequest(setId));
        startCycleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var cycleId = (await startCycleResponse.Content.ReadFromJsonAsync<StartTrainingCycleResponse>())!.Id;

        // 5. Il résout le puzzle (bon coup, indice 1 : l'indice 0 a été auto-joué)
        var attemptResponse = await _client.PostAsJsonAsync(
            $"/api/training-cycles/{cycleId}/attempts",
            new SubmitPuzzleAttemptRequest(puzzleId, 1, "g1f3", TimeSpan.FromSeconds(5)));
        attemptResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var attemptResult = await attemptResponse.Content.ReadFromJsonAsync<SubmitPuzzleAttemptResponse>();
        attemptResult!.IsCorrect.Should().BeTrue();
        attemptResult.IsPuzzleComplete.Should().BeTrue();

        // 6. Les stats du cycle reflètent la tentative, et le cycle est auto-clôturé
        // puisque tous les puzzles du set (un seul ici) ont été tentés
        var statsResponse = await _client.GetAsync($"/api/training-cycles/{cycleId}/stats");
        statsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var stats = await statsResponse.Content.ReadFromJsonAsync<CycleStatsDto>();
        stats!.SuccessRate.Should().Be(1.0);
        stats.PuzzleCount.Should().Be(1);

        // 7. Un autre membre ne peut pas accéder au set ni au cycle du premier membre (IDOR)
        var otherMemberEmail = $"{Guid.NewGuid()}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(otherMemberEmail, "OtherPass123"));
        var otherMemberToken = await LoginAsync(otherMemberEmail, "OtherPass123");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherMemberToken);

        var otherReadsSet = await _client.GetAsync($"/api/puzzle-sets/{setId}");
        otherReadsSet.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var otherReadsStats = await _client.GetAsync($"/api/training-cycles/{cycleId}/stats");
        otherReadsStats.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var otherStartsCycle = await _client.PostAsJsonAsync(
            "/api/training-cycles",
            new StartTrainingCycleRequest(setId));
        otherStartsCycle.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
