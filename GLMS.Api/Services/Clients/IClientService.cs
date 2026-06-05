using GLMS.Shared.DTOs;

namespace GLMS.Api.Services.Clients;

public interface IClientService
{
    Task<IReadOnlyList<ClientListItemDto>> GetListAsync(CancellationToken cancellationToken = default);
    Task<ClientEditorDto?> GetEditorAsync(int clientId, CancellationToken cancellationToken = default);
    Task<int> SaveAsync(ClientUpsertCommand command, CancellationToken cancellationToken = default);
}
