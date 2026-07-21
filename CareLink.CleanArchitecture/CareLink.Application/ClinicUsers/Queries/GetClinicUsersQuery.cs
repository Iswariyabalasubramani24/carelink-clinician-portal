using CareLink.Application.Common.Interfaces;
using MediatR;

namespace CareLink.Application.ClinicUsers.Queries;

public class GetClinicUsersQuery : IRequest<List<ClinicUserDto>>
{
    public int TenantId { get; set; }

    public GetClinicUsersQuery() { }

    public GetClinicUsersQuery(int tenantId)
    {
        TenantId = tenantId;
    }
}

public class GetClinicUsersQueryHandler(IClinicianRepository clinicianRepository)
    : IRequestHandler<GetClinicUsersQuery, List<ClinicUserDto>>
{
    public async Task<List<ClinicUserDto>> Handle(GetClinicUsersQuery request, CancellationToken cancellationToken)
    {
        var clinicians = await clinicianRepository.GetByTenantIdAsync(request.TenantId);
        return clinicians.Select(ClinicUserDto.FromEntity).ToList();
    }
}
