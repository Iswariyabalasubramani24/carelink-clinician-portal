using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CareLink.Infrastructure.Repositories;

public class PatientRepository(ApplicationDbContext db) : IPatientRepository
{
    public async Task<Patient> AddAsync(Patient patient)
    {
        db.Patients.Add(patient);
        await db.SaveChangesAsync();
        return patient;
    }

    public async Task<List<Patient>> GetByTenantIdAsync(int tenantId)
    {
        return await db.Patients
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId)
            .OrderBy(p => p.LastName)
            .ToListAsync();
    }
}
