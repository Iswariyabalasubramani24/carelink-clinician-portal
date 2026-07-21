using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using MediatR;

namespace CareLink.Application.ClinicUsers.Commands;

public class CreateClinicUserCommand : IRequest<CreateClinicUserResultDto>
{
    public int TenantId { get; set; }

    // The admin performing the action (from JWT), not the user being created.
    public int ActingClinicianId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public ClinicianRole Role { get; set; } = ClinicianRole.Clinician;

    public string LanguageCode { get; set; } = "en";
}

public class CreateClinicUserCommandHandler(
    IClinicianRepository clinicianRepository,
    ITemporaryPasswordGenerator temporaryPasswordGenerator,
    IPasswordHasher passwordHasher,
    IAuditLogger auditLogger)
    : IRequestHandler<CreateClinicUserCommand, CreateClinicUserResultDto>
{
    public async Task<CreateClinicUserResultDto> Handle(CreateClinicUserCommand request, CancellationToken cancellationToken)
    {
        var existing = await clinicianRepository.GetByEmailAsync(request.Email);
        if (existing is not null)
        {
            throw new EmailAlreadyInUseException();
        }

        var temporaryPassword = temporaryPasswordGenerator.Generate();

        var clinician = new Clinician
        {
            TenantId = request.TenantId,
            Email = request.Email,
            PasswordHash = passwordHasher.Hash(temporaryPassword),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = request.Role,
            LanguageCode = request.LanguageCode,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var created = await clinicianRepository.AddAsync(clinician);

        // Details deliberately exclude the temporary password - it must never
        // be persisted anywhere, including audit logs.
        await auditLogger.LogAsync(
            request.ActingClinicianId, request.TenantId, "ClinicUserCreated", "Clinician", created.Id,
            $"{created.Email} ({created.Role})");

        return new CreateClinicUserResultDto(ClinicUserDto.FromEntity(created), temporaryPassword);
    }
}
