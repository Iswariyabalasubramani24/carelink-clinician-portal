using CareLink.Application.Common.Interfaces;
using CareLink.Application.Patients;
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

    public async Task<Patient?> GetByIdAsync(int id, int tenantId)
    {
        return await db.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);
    }

    public async Task<List<Patient>> SearchAsync(int tenantId, PatientSearchFilters filters)
    {
        var query = db.Patients.AsNoTracking().Where(p => p.TenantId == tenantId);
        query = PatientSearchFilters.Apply(query, filters);

        return await query.OrderBy(p => p.LastName).ToListAsync();
    }
}
