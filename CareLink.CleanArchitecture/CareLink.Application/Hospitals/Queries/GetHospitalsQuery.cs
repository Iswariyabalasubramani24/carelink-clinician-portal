using CareLink.Application.Common.Interfaces;
using MediatR;

namespace CareLink.Application.Hospitals.Queries;

// Super-admin view of every real hospital (active or not), with its clinician
// headcount. Excludes the hidden system tenant, which is infrastructure.
public class GetHospitalsQuery : IRequest<List<HospitalDto>>
{
}

public class GetHospitalsQueryHandler(ITenantRepository tenantRepository) : IRequestHandler<GetHospitalsQuery, List<HospitalDto>>
{
    public async Task<List<HospitalDto>> Handle(GetHospitalsQuery request, CancellationToken cancellationToken)
    {
        var rows = await tenantRepository.GetAllWithClinicianCountAsync();

        return rows
            .Select(r => new HospitalDto(
                r.Tenant.Id,
                r.Tenant.Name,
                r.Tenant.Region,
                r.Tenant.LanguageCode,
                r.Tenant.IsActive,
                r.ClinicianCount))
            .ToList();
    }
}
