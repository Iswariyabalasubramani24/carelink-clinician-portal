using Asp.Versioning;
using CareLink.API.Contracts;
using CareLink.Application.Alerts;
using CareLink.Application.Alerts.Commands;
using CareLink.Application.Alerts.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareLink.API.Controllers;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/alerts")]
public class AlertsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AlertDto>>> GetActive()
    {
        var result = await mediator.Send(new GetActiveAlertsQuery(GetTenantId()));
        return Ok(result);
    }

    [HttpPost("{id}/acknowledge")]
    public async Task<ActionResult<AlertDto>> Acknowledge(int id)
    {
        var result = await mediator.Send(new AcknowledgeAlertCommand
        {
            AlertId = id,
            TenantId = GetTenantId(),
            ClinicianId = GetClinicianId(),
            Action = AlertAcknowledgeAction.Acknowledge
        });
        return Ok(result);
    }

    [HttpPost("{id}/snooze")]
    public async Task<ActionResult<AlertDto>> Snooze(int id)
    {
        var result = await mediator.Send(new AcknowledgeAlertCommand
        {
            AlertId = id,
            TenantId = GetTenantId(),
            ClinicianId = GetClinicianId(),
            Action = AlertAcknowledgeAction.Snooze
        });
        return Ok(result);
    }

    [HttpGet("clinic-settings")]
    public async Task<ActionResult<IEnumerable<ClinicAlertSettingsDto>>> GetClinicSettings()
    {
        var result = await mediator.Send(new GetClinicAlertSettingsQuery(GetTenantId()));
        return Ok(result);
    }

    [HttpPut("clinic-settings")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<ClinicAlertSettingsDto>>> UpdateClinicSettings(UpdateClinicAlertSettingsRequest request)
    {
        var result = await mediator.Send(new UpdateClinicAlertSettingsCommand
        {
            TenantId = GetTenantId(),
            Settings = request.Settings
        });
        return Ok(result);
    }

    private int GetTenantId()
    {
        var claim = User.FindFirst("tenantId")?.Value;
        return int.Parse(claim!);
    }

    private int GetClinicianId()
    {
        var claim = User.FindFirst("clinicianId")?.Value;
        return int.Parse(claim!);
    }
}
