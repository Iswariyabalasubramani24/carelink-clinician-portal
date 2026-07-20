using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CareLink.Infrastructure.Repositories;

public class ClinicianTenantRepository(ApplicationDbContext db) : IClinicianTenantRepository
{
    public async Task<bool> HasAccessAsync(int clinicianId, int tenantId)
    {
        return await db.ClinicianTenants
            .AnyAsync(ct => ct.ClinicianId == clinicianId && ct.TenantId == tenantId);
    }

    public async Task<List<Tenant>> GetTenantsForClinicianAsync(int clinicianId)
    {
        return await db.ClinicianTenants
            .AsNoTracking()
            .Where(ct => ct.ClinicianId == clinicianId && ct.Tenant.IsActive)
            .OrderBy(ct => ct.Tenant.Name)
            .Select(ct => ct.Tenant)
            .ToListAsync();
    }
}
