using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CareLink.Infrastructure.Repositories;

public class ClinicianRepository(ApplicationDbContext db) : IClinicianRepository
{
    public async Task<Clinician?> GetByEmailAsync(string email)
    {
        return await db.Clinicians.FirstOrDefaultAsync(c => c.Email == email);
    }

    public async Task<Clinician?> GetByIdAsync(int id)
    {
        return await db.Clinicians.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Clinician?> GetByIdAsync(int id, int tenantId)
    {
        return await db.Clinicians.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId);
    }

    public async Task<List<Clinician>> GetByIdsAsync(List<int> ids)
    {
        return await db.Clinicians.AsNoTracking().Where(c => ids.Contains(c.Id)).ToListAsync();
    }

    public async Task<List<Clinician>> GetByTenantIdAsync(int tenantId)
    {
        return await db.Clinicians
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId)
            .OrderBy(c => c.LastName)
            .ToListAsync();
    }

    public async Task<Clinician> AddAsync(Clinician clinician)
    {
        db.Clinicians.Add(clinician);
        await db.SaveChangesAsync();
        return clinician;
    }

    public async Task UpdateAsync(Clinician clinician)
    {
        db.Clinicians.Update(clinician);
        await db.SaveChangesAsync();
    }
}
