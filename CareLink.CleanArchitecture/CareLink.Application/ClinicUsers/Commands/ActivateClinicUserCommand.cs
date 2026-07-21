using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using MediatR;

namespace CareLink.Application.ClinicUsers.Commands;

public class ActivateClinicUserCommand : IRequest<ClinicUserDto>
{
    public int ClinicianId { get; set; }

    public int TenantId { get; set; }

    public ActivateClinicUserCommand() { }

    public ActivateClinicUserCommand(int clinicianId, int tenantId)
    {
        ClinicianId = clinicianId;
        TenantId = tenantId;
    }
}

public class ActivateClinicUserCommandHandler(IClinicianRepository clinicianRepository)
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

        return ClinicUserDto.FromEntity(clinician);
    }
}
