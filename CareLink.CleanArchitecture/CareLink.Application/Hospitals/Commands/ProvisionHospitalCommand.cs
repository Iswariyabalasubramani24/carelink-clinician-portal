using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using MediatR;

namespace CareLink.Application.Hospitals.Commands;

public class ProvisionHospitalCommand : IRequest<ProvisionHospitalResultDto>
{
    // The super-admin performing the action (from JWT). Recorded on the audit
    // trail; never trusted from the client body.
    public int ActingClinicianId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Region { get; set; } = string.Empty;

    public string LanguageCode { get; set; } = "en";

    public string AdminFirstName { get; set; } = string.Empty;

    public string AdminLastName { get; set; } = string.Empty;

    public string AdminEmail { get; set; } = string.Empty;
}

public class ProvisionHospitalCommandHandler(
    IApplicationDbContext db,
    IClinicianRepository clinicianRepository,
    ITemporaryPasswordGenerator temporaryPasswordGenerator,
    IPasswordHasher passwordHasher,
    IAuditLogger auditLogger)
    : IRequestHandler<ProvisionHospitalCommand, ProvisionHospitalResultDto>
{
    // Sensible clinic-wide defaults so a brand-new hospital works immediately,
    // matching the values every existing tenant was seeded with.
    private static readonly (AlertType Type, AlertUrgency Urgency)[] DefaultAlertSettings =
    {
        (AlertType.DisconnectedMonitor, AlertUrgency.Red),
        (AlertType.LowBattery, AlertUrgency.Yellow),
        (AlertType.IrregularHeartbeat, AlertUrgency.Yellow)
    };

    private const int DefaultIntervalDays = 30;

    public async Task<ProvisionHospitalResultDto> Handle(ProvisionHospitalCommand request, CancellationToken cancellationToken)
    {
        var existing = await clinicianRepository.GetByEmailAsync(request.AdminEmail);
        if (existing is not null)
        {
            throw new EmailAlreadyInUseException();
        }

        var temporaryPassword = temporaryPasswordGenerator.Generate();

        var tenant = new Tenant
        {
            Name = request.Name,
            Region = request.Region,
            LanguageCode = request.LanguageCode,
            IsActive = true,
            IsSystem = false,
            CreatedAt = DateTime.UtcNow
        };
        db.Tenants.Add(tenant);

        // The hospital's first Admin. They receive a one-time temporary password
        // and, once logged in, manage their own clinicians via Clinic Management.
        var admin = new Clinician
        {
            Tenant = tenant,
            Email = request.AdminEmail,
            PasswordHash = passwordHasher.Hash(temporaryPassword),
            FirstName = request.AdminFirstName,
            LastName = request.AdminLastName,
            Role = ClinicianRole.Admin,
            LanguageCode = request.LanguageCode,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Clinicians.Add(admin);

        db.ClinicianTenants.Add(new ClinicianTenant { Clinician = admin, Tenant = tenant });

        foreach (var (type, urgency) in DefaultAlertSettings)
        {
            db.ClinicAlertSettings.Add(new ClinicAlertSettings
            {
                Tenant = tenant,
                AlertType = type,
                DefaultUrgency = urgency
            });
        }

        db.PatientScheduleSettings.Add(new PatientScheduleSettings { Tenant = tenant, IntervalDays = DefaultIntervalDays });
        db.ReportSettings.Add(new ReportSettings { Tenant = tenant, IntervalDays = DefaultIntervalDays });

        // Single SaveChanges commits the whole graph atomically; EF fills the
        // generated tenant/clinician ids into every foreign key via navigations.
        await db.SaveChangesAsync(cancellationToken);

        // Details deliberately exclude the temporary password - it must never be
        // persisted anywhere, including audit logs.
        await auditLogger.LogAsync(
            request.ActingClinicianId, tenant.Id, "TenantProvisioned", "Tenant", tenant.Id,
            $"{tenant.Name} ({tenant.Region})");

        var dto = new HospitalDto(tenant.Id, tenant.Name, tenant.Region, tenant.LanguageCode, tenant.IsActive, ClinicianCount: 1);
        return new ProvisionHospitalResultDto(dto, admin.Email, temporaryPassword);
    }
}
