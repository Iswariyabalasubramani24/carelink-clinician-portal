using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CareLink.Infrastructure.Repositories;

public class TenantRepository(ApplicationDbContext db) : ITenantRepository
{
    public async Task<List<Tenant>> GetAllActiveAsync()
    {
        // !IsSystem is defense in depth: the system tenant is also inactive, but
        // the platform tenant must never surface publicly even if that changes.
        return await db.Tenants
            .AsNoTracking()
            .Where(t => t.IsActive && !t.IsSystem)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<List<(Tenant Tenant, int ClinicianCount)>> GetAllWithClinicianCountAsync()
    {
        var rows = await db.Tenants
            .AsNoTracking()
            .Where(t => !t.IsSystem)
            .OrderBy(t => t.Name)
            .Select(t => new { Tenant = t, Count = db.Clinicians.Count(c => c.TenantId == t.Id) })
            .ToListAsync();

        return rows.Select(r => (r.Tenant, r.Count)).ToList();
    }

    public async Task<Tenant?> GetByIdAsync(int id)
    {
        return await db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);
    }
}
