// src/SprintBoard.Client/Services/Auth.cs
using Microsoft.JSInterop;
using SprintBoard.Contracts;
using SprintBoard.Domain.Common;

namespace SprintBoard.Client.Services;

public sealed record ApiSettings(string BaseUrl);

public sealed class TokenStore(IJSRuntime js)
{
    private const string Key = "sprintboard.token";

    public ValueTask<string?> GetAsync() => js.InvokeAsync<string?>("localStorage.getItem", Key);
    public ValueTask SetAsync(string token) => js.InvokeVoidAsync("localStorage.setItem", Key, token);
    public ValueTask ClearAsync() => js.InvokeVoidAsync("localStorage.removeItem", Key);
}

public sealed class AuthHandler(TokenStore tokens) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var token = await tokens.GetAsync();
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new("Bearer", token);

        return await base.SendAsync(request, ct);
    }
}

public sealed class AuthService(ApiClient api, TokenStore tokens)
{
    public Task<Result<AuthResponse>> LoginAsync(string email, string password) =>
        AuthenticateAsync("api/auth/login", new LoginRequest(email, password));

    public Task<Result<AuthResponse>> RegisterAsync(string email, string password, string name) =>
        AuthenticateAsync("api/auth/register", new RegisterRequest(email, password, name));

    public ValueTask LogoutAsync() => tokens.ClearAsync();

    private async Task<Result<AuthResponse>> AuthenticateAsync(string url, object body)
    {
        var result = await api.PostAsync<AuthResponse>(url, body);
        if (result is Result<AuthResponse>.Ok ok) await tokens.SetAsync(ok.Value.Token);
        return result;
    }
}
