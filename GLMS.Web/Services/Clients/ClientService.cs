using GLMS.Web.Data;
using GLMS.Web.DTOs;
using GLMS.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GLMS.Web.Services.Clients;

/// <summary>
/// service for managing clients
/// 
/// moved out of the page for separation of concerns!
/// </summary>
public class ClientService : IClientService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public ClientService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<IReadOnlyList<ClientListItemDto>> GetListAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Clients
            .AsNoTracking()
            .OrderBy(client => client.Name)
            .Select(client => new ClientListItemDto(
                client.ClientId,
                client.Name,
                client.Email,
                client.Phone,
                client.Region,
                client.Contracts.Count()))
            .ToListAsync(cancellationToken);
    }

    public async Task<ClientEditorDto?> GetEditorAsync(int clientId, CancellationToken cancellationToken = default)
    {
        if (clientId <= 0)
        {
            return null;
        }

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Clients
            .AsNoTracking()
            .Where(item => item.ClientId == clientId)
            .Select(item => new ClientEditorDto(
                item.ClientId,
                item.Name,
                item.Email,
                item.Phone,
                item.Region,
                item.Contracts.Count()))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<int> SaveAsync(ClientUpsertCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            throw new ArgumentException("Client name is required.", nameof(command));
        }

        if (string.IsNullOrWhiteSpace(command.Email))
        {
            throw new ArgumentException("Email address is required.", nameof(command));
        }

        if (string.IsNullOrWhiteSpace(command.Phone))
        {
            throw new ArgumentException("Phone number is required.", nameof(command));
        }

        if (string.IsNullOrWhiteSpace(command.Region))
        {
            throw new ArgumentException("Region is required.", nameof(command));
        }

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        Client client;
        if (command.ClientId.HasValue)
        {
            client = await dbContext.Clients.SingleOrDefaultAsync(item => item.ClientId == command.ClientId.Value, cancellationToken)
                ?? throw new InvalidOperationException("The selected client no longer exists.");
        }
        else
        {
            client = new Client();
            await dbContext.Clients.AddAsync(client, cancellationToken);
        }

        client.Name = command.Name.Trim();
        client.Email = command.Email.Trim();
        client.Phone = command.Phone.Trim();
        client.Region = command.Region.Trim();

        await dbContext.SaveChangesAsync(cancellationToken);
        return client.ClientId;
    }
}
