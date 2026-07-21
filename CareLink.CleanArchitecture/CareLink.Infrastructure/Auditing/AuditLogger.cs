using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;

namespace CareLink.Infrastructure.Auditing;

public class AuditLogger(ApplicationDbContext db) : IAuditLogger
{
    public async Task LogAsync(int clinicianId, int tenantId, string action, string entityType, int entityId, string? details = null)
    {
        db.AuditLogs.Add(new AuditLog
        {
            ClinicianId = clinicianId,
            TenantId = tenantId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Timestamp = DateTime.UtcNow,
            Details = details
        });

        await db.SaveChangesAsync();
    }
}
