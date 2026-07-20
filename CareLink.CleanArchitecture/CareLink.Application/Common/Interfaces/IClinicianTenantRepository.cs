using CareLink.Domain.Entities;

namespace CareLink.Application.Common.Interfaces;

public interface IClinicianTenantRepository
{
    Task<bool> HasAccessAsync(int clinicianId, int tenantId);

    Task<List<Tenant>> GetTenantsForClinicianAsync(int clinicianId);
}
