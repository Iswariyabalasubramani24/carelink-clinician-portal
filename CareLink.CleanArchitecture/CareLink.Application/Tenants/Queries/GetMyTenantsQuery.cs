using CareLink.Application.Common.Interfaces;
using MediatR;

namespace CareLink.Application.Tenants.Queries;

public class GetMyTenantsQuery : IRequest<List<TenantDto>>
{
    // Set server-side from the authenticated access token's claims.
    public int ClinicianId { get; set; }
}

public class GetMyTenantsQueryHandler(IClinicianTenantRepository clinicianTenantRepository)
    : IRequestHandler<GetMyTenantsQuery, List<TenantDto>>
{
    public async Task<List<TenantDto>> Handle(GetMyTenantsQuery request, CancellationToken cancellationToken)
    {
        var tenants = await clinicianTenantRepository.GetTenantsForClinicianAsync(request.ClinicianId);

        return tenants.Select(TenantDto.FromEntity).ToList();
    }
}
