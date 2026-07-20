using CareLink.Domain.Entities;

namespace CareLink.Application.Tenants;

public record TenantDto(int Id, string Name, string Region, string LanguageCode)
{
    public static TenantDto FromEntity(Tenant tenant) => new(
        tenant.Id,
        tenant.Name,
        tenant.Region,
        tenant.LanguageCode);
}
