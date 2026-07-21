namespace CareLink.Application.Common.Interfaces;

public interface IAuditLogger
{
    Task LogAsync(int clinicianId, int tenantId, string action, string entityType, int entityId, string? details = null);
}
