using CareLink.Application.Common.Interfaces;
using MediatR;

namespace CareLink.Application.Tenants.Queries;

public class GetTenantsQuery : IRequest<List<TenantDto>>
{
}

public class GetTenantsQueryHandler(ITenantRepository tenantRepository) : IRequestHandler<GetTenantsQuery, List<TenantDto>>
{
    public async Task<List<TenantDto>> Handle(GetTenantsQuery request, CancellationToken cancellationToken)
    {
        var tenants = await tenantRepository.GetAllActiveAsync();

        return tenants.Select(TenantDto.FromEntity).ToList();
    }
}
