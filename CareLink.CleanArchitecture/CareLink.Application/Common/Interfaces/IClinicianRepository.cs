using CareLink.Domain.Entities;

namespace CareLink.Application.Common.Interfaces;

public interface IClinicianRepository
{
    Task<Clinician?> GetByEmailAsync(string email);

    Task<Clinician?> GetByIdAsync(int id);

    // Bulk lookup so resolving clinician names for a notes list doesn't
    // issue one query per note.
    Task<List<Clinician>> GetByIdsAsync(List<int> ids);
}
