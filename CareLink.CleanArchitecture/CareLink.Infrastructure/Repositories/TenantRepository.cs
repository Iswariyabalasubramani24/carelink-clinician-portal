using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CareLink.Infrastructure.Repositories;

public class TenantRepository(ApplicationDbContext db) : ITenantRepository
{
    public async Task<List<Tenant>> GetAllActiveAsync()
    {
        return await db.Tenants
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }
}
