using GLMS.Api.Data;
using GLMS.Api.Models;
using GLMS.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace GLMS.Api.Services.Clients;

public class ClientService : IClientService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public ClientService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<IReadOnlyList<ClientListItemDto>> GetListAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Clients
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new ClientListItemDto(
                c.ClientId, c.Name, c.Email, c.Phone, c.Region,
                c.Contracts.Count()))
            .ToListAsync(cancellationToken);
    }

    public async Task<ClientEditorDto?> GetEditorAsync(int clientId, CancellationToken cancellationToken = default)
    {
        if (clientId <= 0) return null;
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Clients
            .AsNoTracking()
            .Where(c => c.ClientId == clientId)
            .Select(c => new ClientEditorDto(
                c.ClientId, c.Name, c.Email, c.Phone, c.Region,
                c.Contracts.Count()))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<int> SaveAsync(ClientUpsertCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            throw new ArgumentException("Client name is required.", nameof(command));
        if (string.IsNullOrWhiteSpace(command.Email))
            throw new ArgumentException("Email address is required.", nameof(command));
        if (string.IsNullOrWhiteSpace(command.Phone))
            throw new ArgumentException("Phone number is required.", nameof(command));
        if (string.IsNullOrWhiteSpace(command.Region))
            throw new ArgumentException("Region is required.", nameof(command));

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        Client client;
        if (command.ClientId.HasValue)
        {
            client = await db.Clients.SingleOrDefaultAsync(c => c.ClientId == command.ClientId.Value, cancellationToken)
                ?? throw new InvalidOperationException("The selected client no longer exists.");
        }
        else
        {
            client = new Client();
            await db.Clients.AddAsync(client, cancellationToken);
        }

        client.Name = command.Name.Trim();
        client.Email = command.Email.Trim();
        client.Phone = command.Phone.Trim();
        client.Region = command.Region.Trim();

        await db.SaveChangesAsync(cancellationToken);
        return client.ClientId;
    }
}
