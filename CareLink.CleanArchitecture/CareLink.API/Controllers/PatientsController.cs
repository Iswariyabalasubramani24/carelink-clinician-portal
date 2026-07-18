using CareLink.Application.Patients;
using CareLink.Application.Patients.Commands;
using CareLink.Application.Patients.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CareLink.API.Controllers;

[ApiController]
[Route("api/patients")]
public class PatientsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<PatientDto>> Create(CreatePatientCommand command)
    {
        var result = await mediator.Send(command);
        return CreatedAtAction(nameof(GetByTenant), new { tenantId = result.TenantId }, result);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PatientDto>>> GetByTenant([FromQuery] int tenantId)
    {
        var result = await mediator.Send(new GetPatientsQuery(tenantId));
        return Ok(result);
    }
}
