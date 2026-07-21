using Asp.Versioning;
using CareLink.API.Contracts;
using CareLink.Application.Hospitals;
using CareLink.Application.Hospitals.Commands;
using CareLink.Application.Hospitals.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareLink.API.Controllers;

// Platform operations, restricted to the SuperAdmin role. This is how new
// hospitals (tenants) are onboarded - there is no self-service tenant creation
// for regular clinic Admins, who are scoped to their own hospital.
[Authorize(Roles = "SuperAdmin")]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/hospitals")]
public class HospitalsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<HospitalDto>>> GetAll()
    {
        var result = await mediator.Send(new GetHospitalsQuery());
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ProvisionHospitalResultDto>> Provision(ProvisionHospitalRequest request)
    {
        var result = await mediator.Send(new ProvisionHospitalCommand
        {
            ActingClinicianId = GetClinicianId(),
            Name = request.Name,
            Region = request.Region,
            LanguageCode = request.LanguageCode,
            AdminFirstName = request.AdminFirstName,
            AdminLastName = request.AdminLastName,
            AdminEmail = request.AdminEmail
        });
        return Ok(result);
    }

    private int GetClinicianId()
    {
        var claim = User.FindFirst("clinicianId")?.Value;
        return int.Parse(claim!);
    }
}
