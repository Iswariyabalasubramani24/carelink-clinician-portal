using CareLink.API.Contracts;
using CareLink.Application.Alerts;
using CareLink.Application.Alerts.Commands;
using CareLink.Application.Alerts.Queries;
using CareLink.Application.Patients;
using CareLink.Application.Patients.Commands;
using CareLink.Application.Patients.Queries;
using CareLink.Application.Reports;
using CareLink.Application.Reports.Commands;
using CareLink.Application.Reports.Queries;
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

    [HttpGet("{id}/transmissions")]
    public async Task<ActionResult<IEnumerable<TransmissionHistoryDto>>> GetTransmissionHistory(int id)
    {
        var result = await mediator.Send(new GetPatientTransmissionHistoryQuery(id, GetTenantId()));
        return Ok(result);
    }

    [HttpGet("{id}/alerts")]
    public async Task<ActionResult<IEnumerable<AlertDto>>> GetAlerts(int id)
    {
        var result = await mediator.Send(new GetPatientAlertsQuery(id, GetTenantId()));
        return Ok(result);
    }

    [HttpGet("{id}/alert-settings")]
    public async Task<ActionResult<IEnumerable<PatientAlertSettingsDto>>> GetAlertSettings(int id)
    {
        var result = await mediator.Send(new GetPatientAlertSettingsQuery(id, GetTenantId()));
        return Ok(result);
    }

    [HttpPut("{id}/alert-settings")]
    public async Task<ActionResult<IEnumerable<PatientAlertSettingsDto>>> UpdateAlertSettings(int id, UpdatePatientAlertSettingsRequest request)
    {
        var result = await mediator.Send(new UpdatePatientAlertSettingsCommand
        {
            PatientId = id,
            TenantId = GetTenantId(),
            UseOverride = request.UseOverride,
            Overrides = request.Overrides
        });
        return Ok(result);
    }

    [HttpGet("{id}/reports")]
    public async Task<ActionResult<IEnumerable<ReportDto>>> GetReports(int id)
    {
        var result = await mediator.Send(new GetPatientReportsQuery(id, GetTenantId()));
        return Ok(result);
    }

    [HttpPost("{id}/reports")]
    public async Task<ActionResult<ReportDto>> GenerateReport(int id, GenerateReportRequest request)
    {
        var result = await mediator.Send(new GenerateReportCommand(id, GetTenantId(), request.ReportType));
        return Ok(result);
    }

    [HttpGet("{id}/report-settings")]
    public async Task<ActionResult<PatientReportSettingsDto>> GetReportSettings(int id)
    {
        var result = await mediator.Send(new GetPatientReportSettingsQuery(id, GetTenantId()));
        return Ok(result);
    }

    [HttpPut("{id}/report-settings")]
    public async Task<ActionResult<PatientReportSettingsDto>> UpdateReportSettings(int id, UpdatePatientReportSettingsRequest request)
    {
        var result = await mediator.Send(new UpdatePatientReportSettingsCommand
        {
            PatientId = id,
            TenantId = GetTenantId(),
            UseOverride = request.UseOverride,
            IntervalDays = request.IntervalDays
        });
        return Ok(result);
    }

    private int GetTenantId()
    {
        var claim = User.FindFirst("tenantId")?.Value;
        return int.Parse(claim!);
    }
}
