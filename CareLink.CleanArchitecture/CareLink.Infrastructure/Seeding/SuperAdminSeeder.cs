using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CareLink.Infrastructure.Seeding;

// Bootstraps the one account that cannot be created through the UI: the first
// platform SuperAdmin. Runs in every environment and is idempotent - once a
// SuperAdmin exists it does nothing. Credentials come from configuration
// (SuperAdmin:Email / SuperAdmin:Password), which in production must be
// supplied via environment variables or a secret store, never committed.
public class SuperAdminSeeder(
    ApplicationDbContext db,
    IPasswordHasher passwordHasher,
    ILogger<SuperAdminSeeder> logger)
{
    private const string SystemTenantName = "System Administration";

    public async Task SeedAsync(string? email, string? password)
    {
        if (await db.Clinicians.AnyAsync(c => c.Role == ClinicianRole.SuperAdmin))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "No SuperAdmin exists and none is configured (SuperAdmin:Email / SuperAdmin:Password). " +
                "Set these to bootstrap the platform operator account.");
            return;
        }

        // The hidden system tenant anchors SuperAdmin accounts. IsActive=false and
        // IsSystem=true keep it out of the login picker, clinic switcher, and the
        // hospital-management list.
        var systemTenant = await db.Tenants.FirstOrDefaultAsync(t => t.IsSystem);
        if (systemTenant is null)
        {
            systemTenant = new Tenant
            {
                Name = SystemTenantName,
                Region = "System",
                LanguageCode = "en",
                IsActive = false,
                IsSystem = true,
                CreatedAt = DateTime.UtcNow
            };
            db.Tenants.Add(systemTenant);
        }

        db.Clinicians.Add(new Clinician
        {
            Tenant = systemTenant,
            Email = email.Trim(),
            PasswordHash = passwordHasher.Hash(password),
            FirstName = "Platform",
            LastName = "Administrator",
            Role = ClinicianRole.SuperAdmin,
            LanguageCode = "en",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(CancellationToken.None);
        logger.LogInformation("Seeded initial SuperAdmin account for {Email}.", email.Trim());
    }
}
