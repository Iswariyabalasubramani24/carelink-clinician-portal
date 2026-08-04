using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using MediatR;

namespace CareLink.Application.ClinicUsers.Commands;

// Closes the account-recovery gap: a clinician who has lost or forgotten a
// one-time temporary password (or their regular password) cannot use
// ChangePasswordCommand, which requires knowing the current one. An Admin in
// the same hospital re-issues a fresh temporary password instead - the same
// mechanism used at account creation.
public class ResetClinicianPasswordCommand : IRequest<ResetClinicianPasswordResultDto>
{
    public int ClinicianId { get; set; }

    public int TenantId { get; set; }

    // The admin performing the action (from JWT), not the user being reset.
    public int ActingClinicianId { get; set; }
}

public class ResetClinicianPasswordCommandHandler(
    IClinicianRepository clinicianRepository,
    ITemporaryPasswordGenerator temporaryPasswordGenerator,
    IPasswordHasher passwordHasher,
    IAuditLogger auditLogger) : IRequestHandler<ResetClinicianPasswordCommand, ResetClinicianPasswordResultDto>
{
    public async Task<ResetClinicianPasswordResultDto> Handle(ResetClinicianPasswordCommand request, CancellationToken cancellationToken)
    {
        var clinician = await clinicianRepository.GetByIdAsync(request.ClinicianId, request.TenantId)
            ?? throw new ClinicianNotFoundException();

        var temporaryPassword = temporaryPasswordGenerator.Generate();
        clinician.PasswordHash = passwordHasher.Hash(temporaryPassword);
        await clinicianRepository.UpdateAsync(clinician);

        // Details deliberately exclude the temporary password - it must never
        // be persisted anywhere, including audit logs.
        await auditLogger.LogAsync(
            request.ActingClinicianId, request.TenantId, "PasswordReset", "Clinician", clinician.Id,
            clinician.Email);

        return new ResetClinicianPasswordResultDto(ClinicUserDto.FromEntity(clinician), temporaryPassword);
    }
}
