using CareLink.Domain.Entities;

namespace CareLink.Application.Common.Interfaces;

public interface IPatientRepository
{
    Task<Patient> AddAsync(Patient patient);

    Task<List<Patient>> GetByTenantIdAsync(int tenantId);
}
