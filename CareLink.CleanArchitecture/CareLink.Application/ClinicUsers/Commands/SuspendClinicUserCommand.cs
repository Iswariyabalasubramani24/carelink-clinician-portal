using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using MediatR;

namespace CareLink.Application.ClinicUsers.Commands;

public class SuspendClinicUserCommand : IRequest<ClinicUserDto>
{
    public int ClinicianId { get; set; }

    public int TenantId { get; set; }

    // The admin performing the action (from JWT), not the user being suspended.
    public int ActingClinicianId { get; set; }

    public SuspendClinicUserCommand() { }

    public SuspendClinicUserCommand(int clinicianId, int tenantId, int actingClinicianId = 0)
    {
        ClinicianId = clinicianId;
        TenantId = tenantId;
        ActingClinicianId = actingClinicianId;
    }
}

public class SuspendClinicUserCommandHandler(IClinicianRepository clinicianRepository, IAuditLogger auditLogger)
    : IRequestHandler<SuspendClinicUserCommand, ClinicUserDto>
{
    public async Task<ClinicUserDto> Handle(SuspendClinicUserCommand request, CancellationToken cancellationToken)
    {
        var clinician = await clinicianRepository.GetByIdAsync(request.ClinicianId, request.TenantId);
        if (clinician is null)
        {
            throw new ClinicianNotFoundException();
        }

        clinician.IsActive = false;
        await clinicianRepository.UpdateAsync(clinician);

        await auditLogger.LogAsync(
            request.ActingClinicianId, request.TenantId, "ClinicUserSuspended", "Clinician", clinician.Id,
            clinician.Email);

        return ClinicUserDto.FromEntity(clinician);
    }
}
