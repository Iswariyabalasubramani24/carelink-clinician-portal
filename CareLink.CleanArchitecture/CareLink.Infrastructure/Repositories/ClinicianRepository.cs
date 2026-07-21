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

    public async Task<List<Clinician>> GetByIdsAsync(List<int> ids)
    {
        return await db.Clinicians.AsNoTracking().Where(c => ids.Contains(c.Id)).ToListAsync();
    }
}
