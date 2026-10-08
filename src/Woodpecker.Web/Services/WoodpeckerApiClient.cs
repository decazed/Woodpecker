using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Woodpecker.Web.Services;

// AuthState est injecté ici plutôt que dans un DelegatingHandler : avec IHttpClientFactory,
// les handlers HTTP sont construits dans un scope DI interne séparé du scope du circuit
// Blazor, donc un handler ne verrait pas le bon AuthState (scoped) une fois le pool de
// handlers réutilisé. Le typed client lui-même, en revanche, est bien résolu dans le bon
// scope, donc on y attache le token explicitement sur chaque requête.
public class WoodpeckerApiClient(HttpClient http, AuthState authState)
{
    private HttpRequestMessage CreateRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        if (authState.Token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authState.Token);

        return request;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        var response = await http.SendAsync(request);
        if (response.IsSuccessStatusCode)
            return response;

        // Token expiré ou invalide (hors tentative de login) : on oublie la session persistée
        // pour ne pas rester bloqué sur un état "connecté" qui échoue à chaque appel.
        if (response.StatusCode == HttpStatusCode.Unauthorized && authState.IsAuthenticated)
            await authState.ClearAsync();

        throw new ApiException(response.StatusCode, await ExtractErrorMessageAsync(response));
    }

    private static async Task<string> ExtractErrorMessageAsync(HttpResponseMessage response)
    {
        try
        {
            using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = problem.RootElement;

            if (root.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
                return detail.GetString()!;

            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                var messages = errors.EnumerateObject()
                    .SelectMany(field => field.Value.EnumerateArray().Select(m => m.GetString()))
                    .Where(m => m is not null);
                return string.Join(" ", messages);
            }

            if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
                return title.GetString()!;
        }
        catch (JsonException)
        {
        }

        return $"Erreur {(int)response.StatusCode} ({response.ReasonPhrase}).";
    }

    public async Task RegisterAsync(string email, string password)
    {
        var request = CreateRequest(HttpMethod.Post, "api/auth/register");
        request.Content = JsonContent.Create(new RegisterRequest(email, password));
        await SendAsync(request);
    }

    public async Task<string> LoginAsync(string email, string password)
    {
        var request = CreateRequest(HttpMethod.Post, "api/auth/login");
        request.Content = JsonContent.Create(new LoginRequest(email, password));
        var response = await SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.Token;
    }

    public async Task<IReadOnlyList<PuzzleSetSummaryDto>> GetMySetsAsync()
    {
        var response = await SendAsync(CreateRequest(HttpMethod.Get, "api/puzzle-sets"));
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<PuzzleSetSummaryDto>>() ?? [];
    }

    public async Task<PuzzleSetDto?> GetSetAsync(Guid id)
    {
        var response = await SendAsync(CreateRequest(HttpMethod.Get, $"api/puzzle-sets/{id}"));
        return await response.Content.ReadFromJsonAsync<PuzzleSetDto>();
    }

    public async Task<IReadOnlyList<CycleProgressionDto>> GetSetProgressionAsync(Guid setId)
    {
        var response = await SendAsync(CreateRequest(HttpMethod.Get, $"api/puzzle-sets/{setId}/progression"));
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<CycleProgressionDto>>() ?? [];
    }

    public async Task<IReadOnlyList<SetTemplateDto>> GetSetTemplatesAsync()
    {
        var response = await SendAsync(CreateRequest(HttpMethod.Get, "api/puzzle-sets/templates"));
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<SetTemplateDto>>() ?? [];
    }

    public async Task<Guid> CreateSetFromTemplateAsync(string templateKey, int puzzleCount)
    {
        var request = CreateRequest(HttpMethod.Post, "api/puzzle-sets/from-template");
        request.Content = JsonContent.Create(new CreatePuzzleSetFromTemplateRequest(templateKey, puzzleCount));
        var response = await SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<CreatePuzzleSetResponse>();
        return body!.Id;
    }

    public async Task<PuzzleDto?> GetPuzzleAsync(Guid id)
    {
        var response = await SendAsync(CreateRequest(HttpMethod.Get, $"api/puzzles/{id}"));
        return await response.Content.ReadFromJsonAsync<PuzzleDto>();
    }

    public async Task<Guid> StartCycleAsync(Guid puzzleSetId)
    {
        var request = CreateRequest(HttpMethod.Post, "api/training-cycles");
        request.Content = JsonContent.Create(new StartTrainingCycleRequest(puzzleSetId));
        var response = await SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<StartTrainingCycleResponse>();
        return body!.Id;
    }

    public async Task<TrainingCycleDto?> GetCycleAsync(Guid cycleId)
    {
        var response = await SendAsync(CreateRequest(HttpMethod.Get, $"api/training-cycles/{cycleId}"));
        return await response.Content.ReadFromJsonAsync<TrainingCycleDto>();
    }

    public async Task<SubmitPuzzleAttemptResponse> SubmitAttemptAsync(Guid cycleId, Guid puzzleId, int moveIndex, string move, TimeSpan duration)
    {
        var request = CreateRequest(HttpMethod.Post, $"api/training-cycles/{cycleId}/attempts");
        request.Content = JsonContent.Create(new SubmitPuzzleAttemptRequest(puzzleId, moveIndex, move, duration));
        var response = await SendAsync(request);
        return (await response.Content.ReadFromJsonAsync<SubmitPuzzleAttemptResponse>())!;
    }

    public async Task<CycleDetailDto?> GetCycleDetailAsync(Guid cycleId)
    {
        var response = await SendAsync(CreateRequest(HttpMethod.Get, $"api/training-cycles/{cycleId}/detail"));
        return await response.Content.ReadFromJsonAsync<CycleDetailDto>();
    }

    public async Task AbandonCycleAsync(Guid cycleId) =>
        await SendAsync(CreateRequest(HttpMethod.Post, $"api/training-cycles/{cycleId}/abandon"));

    public async Task DeleteSetAsync(Guid setId) =>
        await SendAsync(CreateRequest(HttpMethod.Delete, $"api/puzzle-sets/{setId}"));

    public async Task<CycleStatsDto?> GetCycleStatsAsync(Guid cycleId)
    {
        var response = await SendAsync(CreateRequest(HttpMethod.Get, $"api/training-cycles/{cycleId}/stats"));
        return await response.Content.ReadFromJsonAsync<CycleStatsDto>();
    }
}
