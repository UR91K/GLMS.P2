using GLMS.Api.Services.ServiceRequests;
using GLMS.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GLMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ServiceRequestsController : ControllerBase
{
    private readonly IServiceRequestService _serviceRequestService;

    public ServiceRequestsController(IServiceRequestService serviceRequestService)
    {
        _serviceRequestService = serviceRequestService;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ServiceRequestListItemDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByContract([FromQuery] int contractId, CancellationToken cancellationToken)
    {
        var requests = await _serviceRequestService.GetByContractAsync(contractId, cancellationToken);
        return Ok(requests);
    }

    [HttpPost]
    [ProducesResponseType<ServiceRequestCreationResult>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] ServiceRequestCreateCommand command, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var result = await _serviceRequestService.CreateAsync(command, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("no longer exists"))
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
