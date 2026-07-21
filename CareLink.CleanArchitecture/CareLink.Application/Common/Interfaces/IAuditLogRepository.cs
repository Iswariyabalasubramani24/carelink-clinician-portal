using CareLink.Domain.Entities;

namespace CareLink.Application.Common.Interfaces;

public interface IAuditLogRepository
{
    // Newest first, tenant-scoped, optional action filter, offset pagination.
    Task<(List<AuditLog> Items, int TotalCount)> GetByTenantIdAsync(int tenantId, string? action, int page, int pageSize);
}
