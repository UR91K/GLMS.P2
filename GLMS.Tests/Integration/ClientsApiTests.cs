using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GLMS.Shared.DTOs;

namespace GLMS.Tests.Integration;

public class ClientsApiTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ClientsApiTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task AuthorizeAsync()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin", "Admin@1234"));
        var body = await resp.Content.ReadFromJsonAsync<LoginResponse>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
    }

    [Fact]
    public async Task GetAll_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/clients");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithToken_Returns200AndList()
    {
        await AuthorizeAsync();
        var response = await _client.GetAsync("/api/clients");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var clients = await response.Content.ReadFromJsonAsync<List<ClientListItemDto>>();
        Assert.NotNull(clients);
    }

    [Fact]
    public async Task Create_ThenGet_ReturnsCreatedClient()
    {
        await AuthorizeAsync();

        var command = new ClientUpsertCommand(null, "Test Client", "test@example.com", "+27 11 000 0000", "Gauteng");
        var createResponse = await _client.PostAsJsonAsync("/api/clients", command);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var clientId = await createResponse.Content.ReadFromJsonAsync<int>();
        Assert.True(clientId > 0);

        var getResponse = await _client.GetAsync($"/api/clients/{clientId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var client = await getResponse.Content.ReadFromJsonAsync<ClientEditorDto>();
        Assert.NotNull(client);
        Assert.Equal("Test Client", client.Name);
    }

    [Fact]
    public async Task GetById_NonExistent_Returns404()
    {
        await AuthorizeAsync();
        var response = await _client.GetAsync("/api/clients/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
