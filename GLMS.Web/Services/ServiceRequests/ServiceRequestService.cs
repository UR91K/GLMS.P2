using System.Net.Http.Headers;
using System.Net.Http.Json;
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

    public async Task<IReadOnlyList<GLMS.Shared.DTOs.ServiceRequestListItemDto>> GetListAsync(CancellationToken cancellationToken = default)
    {
        var client = CreateAuthorizedClient();
        return await client.GetFromJsonAsync<IReadOnlyList<GLMS.Shared.DTOs.ServiceRequestListItemDto>>("/api/servicerequests", cancellationToken) ?? [];
    }

    public async Task<ServiceRequestCreationResult> CreateAsync(ServiceRequestCreateCommand command, CancellationToken cancellationToken = default)
    {
        var client = CreateAuthorizedClient();
        var apiCommand = new GLMS.Shared.DTOs.ServiceRequestCreateCommand(command.ContractId, command.Description, command.CostUsd);
        var response = await client.PostAsJsonAsync("/api/servicerequests", apiCommand, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(errorBody) ? "Failed to create service request." : errorBody);
        }

        var result = await response.Content.ReadFromJsonAsync<GLMS.Shared.DTOs.ServiceRequestCreationResult>(cancellationToken)
            ?? throw new InvalidOperationException("Server returned an empty response.");

        return new ServiceRequestCreationResult(
            result.ServiceRequestId,
            result.ExchangeRate,
            result.CostUsd,
            result.CostZar,
            result.CreatedAtUtc,
            result.ExchangeRateFromCache);
    }
}
