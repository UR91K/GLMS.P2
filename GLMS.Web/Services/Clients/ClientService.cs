using System.Net.Http.Headers;
using System.Net.Http.Json;
using GLMS.Shared.DTOs;
using GLMS.Web.Auth;

namespace GLMS.Web.Services.Clients;

public sealed class ClientService : IClientService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GlmsAuthStateProvider _authProvider;

    public ClientService(IHttpClientFactory httpClientFactory, GlmsAuthStateProvider authProvider)
    {
        _httpClientFactory = httpClientFactory;
        _authProvider = authProvider;
    }

    private HttpClient CreateAuthorizedClient()
    {
        var client = _httpClientFactory.CreateClient("GlmsApi");
        if (_authProvider.Token is not null)
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _authProvider.Token);
        return client;
    }

    public async Task<IReadOnlyList<ClientListItemDto>> GetListAsync(CancellationToken cancellationToken = default)
    {
        var client = CreateAuthorizedClient();
        return await client.GetFromJsonAsync<IReadOnlyList<ClientListItemDto>>("/api/clients", cancellationToken) ?? [];
    }

    public async Task<ClientEditorDto?> GetEditorAsync(int clientId, CancellationToken cancellationToken = default)
    {
        var client = CreateAuthorizedClient();
        var response = await client.GetAsync($"/api/clients/{clientId}", cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ClientEditorDto>(cancellationToken);
    }

    public async Task<int> SaveAsync(ClientUpsertCommand command, CancellationToken cancellationToken = default)
    {
        var client = CreateAuthorizedClient();

        if (command.ClientId is null)
        {
            var response = await client.PostAsJsonAsync("/api/clients", command, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<int>(cancellationToken);
        }
        else
        {
            var response = await client.PutAsJsonAsync($"/api/clients/{command.ClientId.Value}", command, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                throw new InvalidOperationException("The selected client no longer exists.");

            response.EnsureSuccessStatusCode();
            return command.ClientId.Value;
        }
    }
}
