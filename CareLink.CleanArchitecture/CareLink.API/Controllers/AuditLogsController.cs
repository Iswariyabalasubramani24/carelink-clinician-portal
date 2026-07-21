using Asp.Versioning;
using CareLink.Application.AuditLogs;
using CareLink.Application.AuditLogs.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareLink.API.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/audit-logs")]
public class AuditLogsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedAuditLogsDto>> Get([FromQuery] string? action, [FromQuery] int page = 1, [FromQuery] int pageSize = 25)
    {
        var result = await mediator.Send(new GetAuditLogsQuery(GetTenantId(), action, page, pageSize));
        return Ok(result);
    }

    private int GetTenantId()
    {
        var claim = User.FindFirst("tenantId")?.Value;
        return int.Parse(claim!);
    }
}
