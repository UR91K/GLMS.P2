using System.Net.Http.Headers;
using System.Net.Http.Json;
using GLMS.Shared.DTOs;
using GLMS.Web.Auth;

namespace GLMS.Web.Services.ServiceRequests;

public sealed class ServiceRequestService : IServiceRequestService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GlmsAuthStateProvider _authProvider;

    public ServiceRequestService(IHttpClientFactory httpClientFactory, GlmsAuthStateProvider authProvider)
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

    public async Task<IReadOnlyList<ServiceRequestListItemDto>> GetListAsync(CancellationToken cancellationToken = default)
    {
        var client = CreateAuthorizedClient();
        return await client.GetFromJsonAsync<IReadOnlyList<ServiceRequestListItemDto>>("/api/servicerequests", cancellationToken) ?? [];
    }

    public async Task<ServiceRequestCreationResult> CreateAsync(ServiceRequestCreateCommand command, CancellationToken cancellationToken = default)
    {
        var client = CreateAuthorizedClient();
        var response = await client.PostAsJsonAsync("/api/servicerequests", command, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(errorBody) ? "Failed to create service request." : errorBody);
        }

        return await response.Content.ReadFromJsonAsync<ServiceRequestCreationResult>(cancellationToken)
            ?? throw new InvalidOperationException("Server returned an empty response.");
    }
}
