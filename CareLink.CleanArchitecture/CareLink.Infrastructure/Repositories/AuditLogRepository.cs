using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CareLink.Infrastructure.Repositories;

public class AuditLogRepository(ApplicationDbContext db) : IAuditLogRepository
{
    public async Task<(List<AuditLog> Items, int TotalCount)> GetByTenantIdAsync(int tenantId, string? action, int page, int pageSize)
    {
        var query = db.AuditLogs.AsNoTracking().Where(a => a.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(a => a.Action == action);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
