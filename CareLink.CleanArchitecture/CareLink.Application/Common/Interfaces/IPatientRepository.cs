using CareLink.Domain.Entities;

namespace CareLink.Application.Common.Interfaces;

public interface IPatientRepository
{
    Task<Patient> AddAsync(Patient patient);

    Task<List<Patient>> GetByTenantIdAsync(int tenantId);

    // Scoped to tenantId so a patient ID from one hospital can never be looked
    // up by a clinician authenticated against a different tenant.
    Task<Patient?> GetByIdAsync(int id, int tenantId);
}
