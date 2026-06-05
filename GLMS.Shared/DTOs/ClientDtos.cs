namespace GLMS.Shared.DTOs;

public sealed record ClientListItemDto(
    int ClientId,
    string Name,
    string Email,
    string Phone,
    string Region,
    int ContractCount);

public sealed record ClientEditorDto(
    int ClientId,
    string Name,
    string Email,
    string Phone,
    string Region,
    int ContractCount);

public sealed record ClientUpsertCommand(
    int? ClientId,
    string Name,
    string Email,
    string Phone,
    string Region);
