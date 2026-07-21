using Asp.Versioning;
using CareLink.API.Contracts;
using CareLink.Application.Schedule;
using CareLink.Application.Schedule.Commands;
using CareLink.Application.Schedule.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareLink.API.Controllers;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/schedule")]
public class ScheduleController(IMediator mediator) : ControllerBase
{
    [HttpGet("clinic-settings")]
    public async Task<ActionResult<ScheduleSettingsDto>> GetClinicSettings()
    {
        var result = await mediator.Send(new GetClinicScheduleSettingsQuery(GetTenantId()));
        return Ok(result);
    }

    [HttpPut("clinic-settings")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ScheduleSettingsDto>> UpdateClinicSettings(UpdateClinicScheduleSettingsRequest request)
    {
        var result = await mediator.Send(new UpdateClinicScheduleSettingsCommand
        {
            TenantId = GetTenantId(),
            IntervalDays = request.IntervalDays
        });
        return Ok(result);
    }

    [HttpGet("transmission-schedule")]
    public async Task<ActionResult<List<TransmissionScheduleEntryDto>>> GetTransmissionSchedule()
    {
        var result = await mediator.Send(new GetTransmissionScheduleQuery(GetTenantId()));
        return Ok(result);
    }

    private int GetTenantId()
    {
        var claim = User.FindFirst("tenantId")?.Value;
        return int.Parse(claim!);
    }
}
