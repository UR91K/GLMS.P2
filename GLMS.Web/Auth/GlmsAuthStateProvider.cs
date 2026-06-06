using System.Security.Claims;
using System.Text.Json;
using GLMS.Shared.DTOs;
using Microsoft.AspNetCore.Components.Authorization;

namespace GLMS.Web.Auth;

public class GlmsAuthStateProvider : AuthenticationStateProvider
{
    public string? Token { get; private set; }

    private static readonly AuthenticationState _anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    private AuthenticationState _current = _anonymous;

    public Task LoginAsync(LoginResponse response)
    {
        Token = response.Token;
        var claims = ParseClaimsFromJwt(response.Token);
        var identity = new ClaimsIdentity(claims, "jwt");
        _current = new AuthenticationState(new ClaimsPrincipal(identity));
        NotifyAuthenticationStateChanged(Task.FromResult(_current));
        return Task.CompletedTask;
    }

    public Task LogoutAsync()
    {
        Token = null;
        _current = _anonymous;
        NotifyAuthenticationStateChanged(Task.FromResult(_current));
        return Task.CompletedTask;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => Task.FromResult(_current);

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length != 3)
            return [];

        var payload = parts[1];
        switch (payload.Length % 4)
        {
            case 2: payload += "=="; break;
            case 3: payload += "="; break;
        }
        payload = payload.Replace('-', '+').Replace('_', '/');

        using var doc = JsonDocument.Parse(Convert.FromBase64String(payload));
        return doc.RootElement.EnumerateObject()
            .Select(p => new Claim(
                p.Name == "unique_name" ? ClaimTypes.Name : p.Name,
                p.Value.ToString()))
            .ToList();
    }
}
