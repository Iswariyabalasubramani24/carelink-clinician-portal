using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using MediatR;

namespace CareLink.Application.Alerts.Queries;

public class GetPatientAlertsQuery : IRequest<List<AlertDto>>
{
    public int PatientId { get; set; }

    public int TenantId { get; set; }

    public GetPatientAlertsQuery() { }

    public GetPatientAlertsQuery(int patientId, int tenantId)
    {
        PatientId = patientId;
        TenantId = tenantId;
    }
}

public class GetPatientAlertsQueryHandler(IPatientRepository patientRepository, IAlertEvaluationService alertEvaluationService)
    : IRequestHandler<GetPatientAlertsQuery, List<AlertDto>>
{
    public async Task<List<AlertDto>> Handle(GetPatientAlertsQuery request, CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(request.PatientId, request.TenantId);
        if (patient is null)
        {
            throw new PatientNotFoundException();
        }

        var alerts = await alertEvaluationService.GetActiveAlertsForPatientAsync(patient);
        return alerts.Select(AlertDto.FromEntity).ToList();
    }
}
