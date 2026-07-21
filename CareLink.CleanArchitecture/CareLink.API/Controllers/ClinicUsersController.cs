using Asp.Versioning;
using CareLink.API.Contracts;
using CareLink.Application.ClinicUsers;
using CareLink.Application.ClinicUsers.Commands;
using CareLink.Application.ClinicUsers.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareLink.API.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/clinic-users")]
public class ClinicUsersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClinicUserDto>>> GetAll()
    {
        var result = await mediator.Send(new GetClinicUsersQuery(GetTenantId()));
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<CreateClinicUserResultDto>> Create(CreateClinicUserRequest request)
    {
        var result = await mediator.Send(new CreateClinicUserCommand
        {
            TenantId = GetTenantId(),
            ActingClinicianId = GetClinicianId(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            LanguageCode = request.LanguageCode,
            Role = request.Role
        });
        return Ok(result);
    }

    [HttpPut("{id}/suspend")]
    public async Task<ActionResult<ClinicUserDto>> Suspend(int id)
    {
        var result = await mediator.Send(new SuspendClinicUserCommand(id, GetTenantId(), GetClinicianId()));
        return Ok(result);
    }

    [HttpPut("{id}/activate")]
    public async Task<ActionResult<ClinicUserDto>> Activate(int id)
    {
        var result = await mediator.Send(new ActivateClinicUserCommand(id, GetTenantId(), GetClinicianId()));
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
