using CareLink.Domain.Entities;

namespace CareLink.Application.Common.Interfaces;

public interface ITenantRepository
{
    Task<List<Tenant>> GetAllActiveAsync();

    Task<Tenant?> GetByIdAsync(int id);
}
