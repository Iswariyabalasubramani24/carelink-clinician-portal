using CareLink.Application.Tenants;
using CareLink.Application.Tenants.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareLink.API.Controllers;

[ApiController]
[Route("api/tenants")]
public class TenantsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<TenantDto>>> GetAll()
    {
        var result = await mediator.Send(new GetTenantsQuery());
        return Ok(result);
    }

    [HttpGet("mine")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<TenantDto>>> GetMine()
    {
        var clinicianId = int.Parse(User.FindFirst("clinicianId")!.Value);
        var result = await mediator.Send(new GetMyTenantsQuery { ClinicianId = clinicianId });
        return Ok(result);
    }
}
