using GLMS.Api.Services.Contracts;
using GLMS.Shared.DTOs;
using GLMS.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

//references:
// https://medium.com/@jeslurrahman/understanding-cancellation-tokens-in-asp-net-core-a-guide-to-efficient-api-design-39edcd3212a9
// https://refactoring.guru/design-patterns/command
// https://learn.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/security/authorization/simple?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase.physicalfile?view=aspnetcore-10.0
// https://www.rfc-editor.org/info/rfc9110/#section-15
// https://www.rfc-editor.org/info/rfc5789/
// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design

namespace GLMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContractsController : ControllerBase
{
    private readonly IContractService _contractService;
    private readonly IWebHostEnvironment _hostEnvironment;

    public ContractsController(IContractService contractService, IWebHostEnvironment hostEnvironment)
    {
        _contractService = contractService;
        _hostEnvironment = hostEnvironment;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ContractListItemDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] ContractStatus? status,
        [FromQuery] int? clientId,
        CancellationToken cancellationToken)
    {
        var contracts = await _contractService.GetListAsync(status, clientId, cancellationToken);
        return Ok(contracts);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<ContractListItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var contract = await _contractService.GetByIdAsync(id, cancellationToken);
        return contract is null ? NotFound() : Ok(contract);
    }

    [HttpPost]
    [ProducesResponseType<int>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] ContractCreateCommand command, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var contractId = await _contractService.CreateAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = contractId }, contractId);
    }

    [HttpPatch("{id:int}/status")]
    [ProducesResponseType<ContractTransitionResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ContractTransitionResultDto>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeStatus(int id, [FromBody] ContractTransitionCommand command, CancellationToken cancellationToken)
    {
        var result = await _contractService.ChangeStatusAsync(id, command.Action, cancellationToken);
        if (!result.Succeeded && result.PreviousStatus == default && result.CurrentStatus == default)
            return NotFound(result);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id:int}/agreement")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadAgreement(int id, CancellationToken cancellationToken)
    {
        var contract = await _contractService.GetByIdAsync(id, cancellationToken);
        if (contract is null || string.IsNullOrWhiteSpace(contract.PdfFileName))
            return NotFound();

        var root = _hostEnvironment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var path = Path.Combine(root, "uploads", "contracts", contract.PdfFileName);
        if (!System.IO.File.Exists(path))
            return NotFound();

        return PhysicalFile(path, "application/pdf", contract.PdfOriginalFileName ?? contract.PdfFileName);
    }

    [HttpPost("{id:int}/agreement")]
    [ProducesResponseType<ContractAgreementUploadResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ContractAgreementUploadResultDto>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadAgreement(int id, IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new ContractAgreementUploadResultDto(false, id, "No file was uploaded.", null, null));

        await using var stream = file.OpenReadStream();
        var result = await _contractService.UploadAgreementAsync(
            id, file.FileName, file.ContentType, file.Length, stream, cancellationToken);

        if (!result.Succeeded && result.StoredFileName is null)
            return result.Message.Contains("no longer exists") ? NotFound(result) : BadRequest(result);

        return Ok(result);
    }
}
