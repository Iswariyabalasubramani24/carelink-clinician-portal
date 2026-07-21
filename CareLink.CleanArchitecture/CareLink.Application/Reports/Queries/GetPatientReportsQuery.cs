using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using MediatR;

namespace CareLink.Application.Reports.Queries;

public class GetPatientReportsQuery : IRequest<List<ReportDto>>
{
    public int PatientId { get; set; }

    public int TenantId { get; set; }

    public GetPatientReportsQuery() { }

    public GetPatientReportsQuery(int patientId, int tenantId)
    {
        PatientId = patientId;
        TenantId = tenantId;
    }
}

public class GetPatientReportsQueryHandler(IPatientRepository patientRepository, IReportRepository reportRepository)
    : IRequestHandler<GetPatientReportsQuery, List<ReportDto>>
{
    public async Task<List<ReportDto>> Handle(GetPatientReportsQuery request, CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(request.PatientId, request.TenantId);
        if (patient is null)
        {
            throw new PatientNotFoundException();
        }

        var reports = await reportRepository.GetByPatientIdAsync(request.PatientId);
        return reports.Select(ReportDto.FromEntity).ToList();
    }
}
