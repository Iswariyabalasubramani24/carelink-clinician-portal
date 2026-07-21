using CareLink.API.Contracts;
using CareLink.Application.Reports;
using CareLink.Application.Reports.Commands;
using CareLink.Application.Reports.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareLink.API.Controllers;

[Authorize]
[ApiController]
[Route("api/reports")]
public class ReportsController(IMediator mediator) : ControllerBase
{
    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(int id)
    {
        var result = await mediator.Send(new DownloadReportQuery(id, GetTenantId()));
        return File(result.Content, "application/pdf", result.FileName);
    }

    [HttpGet("clinic-settings")]
    public async Task<ActionResult<ReportSettingsDto>> GetClinicSettings()
    {
        var result = await mediator.Send(new GetClinicReportSettingsQuery(GetTenantId()));
        return Ok(result);
    }

    [HttpPut("clinic-settings")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ReportSettingsDto>> UpdateClinicSettings(UpdateClinicReportSettingsRequest request)
    {
        var result = await mediator.Send(new UpdateClinicReportSettingsCommand
        {
            TenantId = GetTenantId(),
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
