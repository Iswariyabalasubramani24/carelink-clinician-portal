using Asp.Versioning;
using CareLink.API.Contracts;
using CareLink.Application.Alerts;
using CareLink.Application.Alerts.Commands;
using CareLink.Application.Alerts.Queries;
using CareLink.Application.Patients;
using CareLink.Application.Patients.Commands;
using CareLink.Application.Patients.Queries;
using CareLink.Application.PatientNotes;
using CareLink.Application.PatientNotes.Commands;
using CareLink.Application.PatientNotes.Queries;
using CareLink.Application.Reports;
using CareLink.Application.Reports.Commands;
using CareLink.Application.Reports.Queries;
using CareLink.Application.Schedule;
using CareLink.Application.Schedule.Commands;
using CareLink.Application.Schedule.Queries;
using CareLink.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareLink.API.Controllers;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/patients")]
public class PatientsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<PatientDto>> Create(CreatePatientCommand command)
    {
        command.TenantId = GetTenantId();
        command.ClinicianId = GetClinicianId();
        var result = await mediator.Send(command);
        return CreatedAtAction(nameof(GetByTenant), result);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PatientDto>>> GetByTenant(
        [FromQuery] DeviceType? deviceType,
        [FromQuery] DateTime? implantDateFrom,
        [FromQuery] DateTime? implantDateTo,
        [FromQuery] bool? isActive,
        [FromQuery] string? keyword)
    {
        var result = await mediator.Send(new GetPatientsQuery(GetTenantId())
        {
            DeviceType = deviceType,
            ImplantDateFrom = implantDateFrom,
            ImplantDateTo = implantDateTo,
            IsActive = isActive,
            Keyword = keyword
        });
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
        var result = await mediator.Send(new GenerateReportCommand(id, GetTenantId(), request.ReportType, GetClinicianId()));
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

    [HttpGet("{id}/schedule-settings")]
    public async Task<ActionResult<PatientScheduleSettingsDto>> GetScheduleSettings(int id)
    {
        var result = await mediator.Send(new GetPatientScheduleSettingsQuery(id, GetTenantId()));
        return Ok(result);
    }

    [HttpPut("{id}/schedule-settings")]
    public async Task<ActionResult<PatientScheduleSettingsDto>> UpdateScheduleSettings(int id, UpdatePatientScheduleSettingsRequest request)
    {
        var result = await mediator.Send(new UpdatePatientScheduleSettingsCommand
        {
            PatientId = id,
            TenantId = GetTenantId(),
            UseOverride = request.UseOverride,
            IntervalDays = request.IntervalDays
        });
        return Ok(result);
    }

    [HttpGet("{id}/notes")]
    public async Task<ActionResult<IEnumerable<PatientNoteDto>>> GetNotes(int id)
    {
        var result = await mediator.Send(new GetPatientNotesQuery(id, GetTenantId()));
        return Ok(result);
    }

    [HttpPost("{id}/notes")]
    public async Task<ActionResult<PatientNoteDto>> CreateNote(int id, CreatePatientNoteRequest request)
    {
        var result = await mediator.Send(new CreatePatientNoteCommand
        {
            PatientId = id,
            TenantId = GetTenantId(),
            ClinicianId = GetClinicianId(),
            Content = request.Content
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
