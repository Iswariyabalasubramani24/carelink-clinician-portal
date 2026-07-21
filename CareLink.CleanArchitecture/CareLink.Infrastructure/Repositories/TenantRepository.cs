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

    public async Task<Tenant?> GetByIdAsync(int id)
    {
        return await db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);
    }
}
