using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using GLMS.Shared.DTOs;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace GLMS.Web.Auth;

public class GlmsAuthStateProvider : AuthenticationStateProvider
{
    private readonly ProtectedLocalStorage _storage;

    public string? Token { get; private set; }

    private static readonly AuthenticationState _anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    private AuthenticationState _current = _anonymous;
    private bool _initialized;

    public GlmsAuthStateProvider(ProtectedLocalStorage storage)
    {
        _storage = storage;
    }

    public async Task LoginAsync(LoginResponse response)
    {
        Token = response.Token;
        await _storage.SetAsync("glms_token", response.Token);
        _current = BuildState(response.Token);
        _initialized = true;
        NotifyAuthenticationStateChanged(Task.FromResult(_current));
    }

    public async Task LogoutAsync()
    {
        Token = null;
        await _storage.DeleteAsync("glms_token");
        _current = _anonymous;
        _initialized = true;
        NotifyAuthenticationStateChanged(Task.FromResult(_current));
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_initialized)
            return _current;

        try
        {
            var result = await _storage.GetAsync<string>("glms_token");
            if (result.Success && !string.IsNullOrEmpty(result.Value))
            {
                Token = result.Value;
                _current = BuildState(result.Value);
            }
        }
        catch (InvalidOperationException)
        {
            // called before the circuit is ready (SSR/prerendering),. JS unavailable.
            // do NOT mark initialized so the interactive circuit will reread the token.
            return _anonymous;
        }
        catch (CryptographicException)
        {
            // stale data protection keys, token cant be decrypted. stay anonymous.
        }

        _initialized = true;
        return _current;
    }

    private AuthenticationState BuildState(string jwt)
    {
        var claims = ParseClaimsFromJwt(jwt);
        var identity = new ClaimsIdentity(claims, "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

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
