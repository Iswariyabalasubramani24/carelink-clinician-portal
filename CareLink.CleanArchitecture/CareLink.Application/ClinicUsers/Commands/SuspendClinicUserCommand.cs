using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using MediatR;

namespace CareLink.Application.ClinicUsers.Commands;

public class SuspendClinicUserCommand : IRequest<ClinicUserDto>
{
    public int ClinicianId { get; set; }

    public int TenantId { get; set; }

    public SuspendClinicUserCommand() { }

    public SuspendClinicUserCommand(int clinicianId, int tenantId)
    {
        ClinicianId = clinicianId;
        TenantId = tenantId;
    }
}

public class SuspendClinicUserCommandHandler(IClinicianRepository clinicianRepository)
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

        return ClinicUserDto.FromEntity(clinician);
    }
}
