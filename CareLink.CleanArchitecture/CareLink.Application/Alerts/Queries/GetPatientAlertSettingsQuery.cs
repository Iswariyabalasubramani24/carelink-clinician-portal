using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using MediatR;

namespace CareLink.Application.Alerts.Queries;

public class GetPatientAlertSettingsQuery : IRequest<List<PatientAlertSettingsDto>>
{
    public int PatientId { get; set; }

    public int TenantId { get; set; }

    public GetPatientAlertSettingsQuery() { }

    public GetPatientAlertSettingsQuery(int patientId, int tenantId)
    {
        PatientId = patientId;
        TenantId = tenantId;
    }
}

public class GetPatientAlertSettingsQueryHandler(
    IPatientRepository patientRepository,
    IClinicAlertSettingsRepository clinicAlertSettingsRepository,
    IPatientAlertSettingsRepository patientAlertSettingsRepository)
    : IRequestHandler<GetPatientAlertSettingsQuery, List<PatientAlertSettingsDto>>
{
    public async Task<List<PatientAlertSettingsDto>> Handle(GetPatientAlertSettingsQuery request, CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(request.PatientId, request.TenantId);
        if (patient is null)
        {
            throw new PatientNotFoundException();
        }

        var clinicSettings = await clinicAlertSettingsRepository.GetByTenantIdAsync(request.TenantId);
        var patientSettings = await patientAlertSettingsRepository.GetByPatientIdAsync(request.PatientId);

        return AlertEvaluationService.ResolvePatientAlertSettings(patient, clinicSettings, patientSettings);
    }
}
