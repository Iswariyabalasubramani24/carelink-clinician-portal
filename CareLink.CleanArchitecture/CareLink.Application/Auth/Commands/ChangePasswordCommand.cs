using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using MediatR;

namespace CareLink.Application.Auth.Commands;

public class ChangePasswordCommand : IRequest
{
    // Both set server-side from JWT claims - a clinician can only ever change
    // their own password.
    public int ClinicianId { get; set; }

    public int TenantId { get; set; }

    public string CurrentPassword { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}

public class ChangePasswordCommandHandler(
    IClinicianRepository clinicianRepository,
    IPasswordHasher passwordHasher,
    IAuditLogger auditLogger) : IRequestHandler<ChangePasswordCommand>
{
    public const int MinPasswordLength = 8;

    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (request.NewPassword.Length < MinPasswordLength)
        {
            throw new ArgumentException($"The new password must be at least {MinPasswordLength} characters long.");
        }

        var clinician = await clinicianRepository.GetByIdAsync(request.ClinicianId)
            ?? throw new ClinicianNotFoundException();

        // Proving knowledge of the current password gates the change - a
        // hijacked unattended session cannot silently take over the account.
        if (!passwordHasher.Verify(request.CurrentPassword, clinician.PasswordHash))
        {
            throw new InvalidCurrentPasswordException();
        }

        clinician.PasswordHash = passwordHasher.Hash(request.NewPassword);
        await clinicianRepository.UpdateAsync(clinician);

        // Details deliberately omit anything password-related.
        await auditLogger.LogAsync(
            request.ClinicianId, request.TenantId, "PasswordChanged", "Clinician", clinician.Id);
    }
}
