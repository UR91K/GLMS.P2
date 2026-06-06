using System.Net.Http.Headers;
using System.Net.Http.Json;
using GLMS.Shared.DTOs;
using GLMS.Shared.Enums;
using GLMS.Web.Auth;

namespace GLMS.Web.Services.Contracts;

public sealed class ContractService : IContractService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GlmsAuthStateProvider _authProvider;

    public ContractService(IHttpClientFactory httpClientFactory, GlmsAuthStateProvider authProvider)
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

    public async Task<IReadOnlyList<ContractListItemDto>> GetListAsync(CancellationToken cancellationToken = default)
    {
        var client = CreateAuthorizedClient();
        return await client.GetFromJsonAsync<IReadOnlyList<ContractListItemDto>>("/api/contracts", cancellationToken) ?? [];
    }

    public async Task<ContractTransitionResultDto> ChangeStatusAsync(
        int contractId,
        ContractTransitionAction action,
        CancellationToken cancellationToken = default)
    {
        var client = CreateAuthorizedClient();
        var response = await client.PatchAsJsonAsync(
            $"/api/contracts/{contractId}/status",
            new ContractTransitionCommand(action),
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ContractTransitionResultDto>(cancellationToken)
            ?? new ContractTransitionResultDto(false, contractId, ContractStatus.Draft, ContractStatus.Draft, "Unexpected empty response.");
    }

    public async Task<ContractAgreementUploadResultDto> UploadAgreementAsync(
        int contractId,
        string originalFileName,
        string? contentType,
        long fileSize,
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        var client = CreateAuthorizedClient();

        using var form = new MultipartFormDataContent();
        var part = new StreamContent(fileStream);
        part.Headers.ContentType = new MediaTypeHeaderValue(contentType ?? "application/pdf");
        form.Add(part, "file", originalFileName);

        var response = await client.PostAsync($"/api/contracts/{contractId}/agreement", form, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ContractAgreementUploadResultDto>(cancellationToken)
            ?? new ContractAgreementUploadResultDto(false, contractId, "Unexpected empty response.", null, null);
    }
}
