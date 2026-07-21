using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using MediatR;

namespace CareLink.Application.Patients.Queries;

public class GetPatientsQuery : IRequest<List<PatientDto>>
{
    public int TenantId { get; set; }

    // Advanced search filters - all optional, combined with AND logic. Left
    // unset, this returns every patient for the tenant (unchanged behavior).
    public DeviceType? DeviceType { get; set; }

    public DateTime? ImplantDateFrom { get; set; }

    public DateTime? ImplantDateTo { get; set; }

    public bool? IsActive { get; set; }

    public string? Keyword { get; set; }

    public GetPatientsQuery() { }

    public GetPatientsQuery(int tenantId)
    {
        TenantId = tenantId;
    }
}

public class GetPatientsQueryHandler(IPatientRepository patientRepository) : IRequestHandler<GetPatientsQuery, List<PatientDto>>
{
    public async Task<List<PatientDto>> Handle(GetPatientsQuery request, CancellationToken cancellationToken)
    {
        var filters = new PatientSearchFilters(
            request.DeviceType,
            request.ImplantDateFrom,
            request.ImplantDateTo,
            request.IsActive,
            request.Keyword);

        var patients = await patientRepository.SearchAsync(request.TenantId, filters);

        return patients.Select(PatientDto.FromEntity).ToList();
    }
}
