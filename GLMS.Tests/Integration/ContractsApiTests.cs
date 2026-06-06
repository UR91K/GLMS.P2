using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GLMS.Shared.DTOs;
using GLMS.Shared.Enums;

namespace GLMS.Tests.Integration;

public class ContractsApiTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ContractsApiTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task AuthorizeAsync()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin", "Admin@1234"));
        var body = await resp.Content.ReadFromJsonAsync<LoginResponse>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
    }

    private async Task<int> CreateClientAsync()
    {
        var resp = await _client.PostAsJsonAsync("/api/clients",
            new ClientUpsertCommand(null, "Contract Test Client", "ct@example.com", "+27 10 000 0000", "Gauteng"));
        return await resp.Content.ReadFromJsonAsync<int>();
    }

    [Fact]
    public async Task GetAll_Returns200AndNonNullJson()
    {
        await AuthorizeAsync();
        var response = await _client.GetAsync("/api/contracts");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var contracts = await response.Content.ReadFromJsonAsync<List<ContractListItemDto>>();
        Assert.NotNull(contracts);
    }

    [Fact]
    public async Task Create_ThenRead_ReturnsCreatedContract()
    {
        await AuthorizeAsync();
        var clientId = await CreateClientAsync();

        var command = new ContractCreateCommand(clientId, "Integration Test Contract", "Gold",
            DateTime.UtcNow, DateTime.UtcNow.AddMonths(12));
        var createResponse = await _client.PostAsJsonAsync("/api/contracts", command);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var contractId = await createResponse.Content.ReadFromJsonAsync<int>();
        Assert.True(contractId > 0);

        var getResponse = await _client.GetAsync($"/api/contracts/{contractId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var contract = await getResponse.Content.ReadFromJsonAsync<ContractListItemDto>();
        Assert.NotNull(contract);
        Assert.Equal("Integration Test Contract", contract.Title);
        Assert.Equal(ContractStatus.Draft, contract.Status);
    }

    [Fact]
    public async Task PatchStatus_ApproveDraftContract_MovesToActive()
    {
        await AuthorizeAsync();
        var clientId = await CreateClientAsync();

        var createResp = await _client.PostAsJsonAsync("/api/contracts",
            new ContractCreateCommand(clientId, "Status Test Contract", "Silver",
                DateTime.UtcNow, DateTime.UtcNow.AddMonths(6)));
        var contractId = await createResp.Content.ReadFromJsonAsync<int>();

        var patchResponse = await _client.PatchAsJsonAsync($"/api/contracts/{contractId}/status",
            new ContractTransitionCommand(ContractTransitionAction.Approve));
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        var result = await patchResponse.Content.ReadFromJsonAsync<ContractTransitionResultDto>();
        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        Assert.Equal(ContractStatus.Draft, result.PreviousStatus);
        Assert.Equal(ContractStatus.Active, result.CurrentStatus);
    }

    [Fact]
    public async Task GetById_NonExistent_Returns404()
    {
        await AuthorizeAsync();
        var response = await _client.GetAsync("/api/contracts/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithStatusFilter_ReturnsFilteredResults()
    {
        await AuthorizeAsync();
        var response = await _client.GetAsync("/api/contracts?status=Draft");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var contracts = await response.Content.ReadFromJsonAsync<List<ContractListItemDto>>();
        Assert.NotNull(contracts);
        Assert.All(contracts, c => Assert.Equal(ContractStatus.Draft, c.Status));
    }
}
