using CareLink.Application.Patients;
using CareLink.Domain.Entities;

namespace CareLink.Application.Common.Interfaces;

public interface IPatientRepository
{
    Task<Patient> AddAsync(Patient patient);

    Task<List<Patient>> GetByTenantIdAsync(int tenantId);

    // Scoped to tenantId so a patient ID from one hospital can never be looked
    // up by a clinician authenticated against a different tenant.
    Task<Patient?> GetByIdAsync(int id, int tenantId);

    // Same tenant scoping as GetByTenantIdAsync, plus optional combinable filters.
    Task<List<Patient>> SearchAsync(int tenantId, PatientSearchFilters filters);

    Task UpdateAsync(Patient patient);
}
