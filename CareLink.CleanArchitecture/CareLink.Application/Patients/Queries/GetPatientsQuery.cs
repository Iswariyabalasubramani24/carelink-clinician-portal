using CareLink.Application.Common.Interfaces;
using MediatR;

namespace CareLink.Application.Patients.Queries;

public class GetPatientsQuery : IRequest<List<PatientDto>>
{
    public int TenantId { get; set; }

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
        var patients = await patientRepository.GetByTenantIdAsync(request.TenantId);

        return patients.Select(PatientDto.FromEntity).ToList();
    }
}
