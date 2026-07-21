using CareLink.Domain.Entities;

namespace CareLink.Application.Common.Interfaces;

public interface ITenantRepository
{
    Task<List<Tenant>> GetAllActiveAsync();

    Task<Tenant?> GetByIdAsync(int id);

    // Super-admin management view: every non-system tenant (active or not)
    // with its clinician headcount, in one query.
    Task<List<(Tenant Tenant, int ClinicianCount)>> GetAllWithClinicianCountAsync();
}
