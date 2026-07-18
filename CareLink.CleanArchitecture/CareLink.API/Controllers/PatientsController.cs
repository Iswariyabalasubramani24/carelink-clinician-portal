using CareLink.Application.Patients;
using CareLink.Application.Patients.Commands;
using CareLink.Application.Patients.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareLink.API.Controllers;

[Authorize]
[ApiController]
[Route("api/patients")]
public class PatientsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<PatientDto>> Create(CreatePatientCommand command)
    {
        command.TenantId = GetTenantId();
        var result = await mediator.Send(command);
        return CreatedAtAction(nameof(GetByTenant), result);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PatientDto>>> GetByTenant()
    {
        var result = await mediator.Send(new GetPatientsQuery(GetTenantId()));
        return Ok(result);
    }

    private int GetTenantId()
    {
        var claim = User.FindFirst("tenantId")?.Value;
        return int.Parse(claim!);
    }
}
