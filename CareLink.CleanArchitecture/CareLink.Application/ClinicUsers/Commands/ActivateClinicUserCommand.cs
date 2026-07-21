using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using MediatR;

namespace CareLink.Application.ClinicUsers.Commands;

public class ActivateClinicUserCommand : IRequest<ClinicUserDto>
{
    public int ClinicianId { get; set; }

    public int TenantId { get; set; }

    // The admin performing the action (from JWT), not the user being activated.
    public int ActingClinicianId { get; set; }

    public ActivateClinicUserCommand() { }

    public ActivateClinicUserCommand(int clinicianId, int tenantId, int actingClinicianId = 0)
    {
        ClinicianId = clinicianId;
        TenantId = tenantId;
        ActingClinicianId = actingClinicianId;
    }
}

public class ActivateClinicUserCommandHandler(IClinicianRepository clinicianRepository, IAuditLogger auditLogger)
    : IRequestHandler<ActivateClinicUserCommand, ClinicUserDto>
{
    public async Task<ClinicUserDto> Handle(ActivateClinicUserCommand request, CancellationToken cancellationToken)
    {
        var clinician = await clinicianRepository.GetByIdAsync(request.ClinicianId, request.TenantId);
        if (clinician is null)
        {
            throw new ClinicianNotFoundException();
        }

        clinician.IsActive = true;
        await clinicianRepository.UpdateAsync(clinician);

        await auditLogger.LogAsync(
            request.ActingClinicianId, request.TenantId, "ClinicUserActivated", "Clinician", clinician.Id,
            clinician.Email);

        return ClinicUserDto.FromEntity(clinician);
    }
}
