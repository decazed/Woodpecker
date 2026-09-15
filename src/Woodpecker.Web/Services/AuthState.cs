using Microsoft.JSInterop;

namespace Woodpecker.Web.Services;

// Scoped : un circuit Blazor Server = un utilisateur, l'état vit en mémoire serveur. Un
// rafraîchissement de page ouvre un nouveau circuit (état perdu), d'où la copie du token dans
// le localStorage du navigateur, relue au premier accès via EnsureRestoredAsync.
public class AuthState(IJSRuntime js)
{
    private const string TokenKey = "woodpecker.token";
    private const string EmailKey = "woodpecker.email";

    private Task? _restore;

    public string? Token { get; private set; }
    public string? Email { get; private set; }
    public bool IsAuthenticated => Token is not null;

    public event Action? OnChange;

    // Idempotent : le premier appelant (layout ou page) lit le localStorage, les suivants
    // attendent la même tâche. À appeler avant de tester IsAuthenticated.
    public Task EnsureRestoredAsync() => _restore ??= RestoreAsync();

    private async Task RestoreAsync()
    {
        try
        {
            var token = await js.InvokeAsync<string?>("localStorage.getItem", TokenKey);
            var email = await js.InvokeAsync<string?>("localStorage.getItem", EmailKey);
            if (token is not null && email is not null)
            {
                Token = token;
                Email = email;
                OnChange?.Invoke();
            }
        }
        catch (JSException)
        {
            // localStorage indisponible (navigation privée stricte, etc.) : session non persistée.
        }
    }

    public async Task SetSessionAsync(string token, string email)
    {
        Token = token;
        Email = email;
        _restore = Task.CompletedTask;
        OnChange?.Invoke();

        try
        {
            await js.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
            await js.InvokeVoidAsync("localStorage.setItem", EmailKey, email);
        }
        catch (JSException)
        {
        }
    }

    public async Task ClearAsync()
    {
        Token = null;
        Email = null;
        OnChange?.Invoke();

        try
        {
            await js.InvokeVoidAsync("localStorage.removeItem", TokenKey);
            await js.InvokeVoidAsync("localStorage.removeItem", EmailKey);
        }
        catch (JSException)
        {
        }
    }
}
