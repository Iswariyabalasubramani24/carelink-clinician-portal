using CareLink.Domain.Entities;

namespace CareLink.Application.Common.Interfaces;

public interface IClinicianRepository
{
    Task<Clinician?> GetByEmailAsync(string email);

    Task<Clinician?> GetByIdAsync(int id);

    // Scoped to tenantId so an admin can never look up (and thus manage) a
    // clinician account belonging to a different hospital.
    Task<Clinician?> GetByIdAsync(int id, int tenantId);

    // Bulk lookup so resolving clinician names for a notes list doesn't
    // issue one query per note.
    Task<List<Clinician>> GetByIdsAsync(List<int> ids);

    Task<List<Clinician>> GetByTenantIdAsync(int tenantId);

    Task<Clinician> AddAsync(Clinician clinician);

    Task UpdateAsync(Clinician clinician);
}
