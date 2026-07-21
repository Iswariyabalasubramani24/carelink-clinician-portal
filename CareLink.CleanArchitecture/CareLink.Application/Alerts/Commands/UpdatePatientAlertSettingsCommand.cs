using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using MediatR;

namespace CareLink.Application.Alerts.Commands;

public class UpdatePatientAlertSettingsCommand : IRequest<List<PatientAlertSettingsDto>>
{
    public int PatientId { get; set; }

    public int TenantId { get; set; }

    // false = revert to clinic defaults for every alert type (overrides are cleared).
    public bool UseOverride { get; set; }

    public List<PatientAlertOverrideInput> Overrides { get; set; } = [];
}

public class UpdatePatientAlertSettingsCommandHandler(
    IPatientRepository patientRepository,
    IClinicAlertSettingsRepository clinicAlertSettingsRepository,
    IPatientAlertSettingsRepository patientAlertSettingsRepository)
    : IRequestHandler<UpdatePatientAlertSettingsCommand, List<PatientAlertSettingsDto>>
{
    public async Task<List<PatientAlertSettingsDto>> Handle(UpdatePatientAlertSettingsCommand request, CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(request.PatientId, request.TenantId);
        if (patient is null)
        {
            throw new PatientNotFoundException();
        }

        var overrides = request.UseOverride
            ? request.Overrides.Select(o => (o.AlertType, o.Urgency)).ToList()
            : new List<(AlertType AlertType, AlertUrgency Urgency)>();

        await patientAlertSettingsRepository.ReplaceOverridesAsync(request.PatientId, overrides);

        var clinicSettings = await clinicAlertSettingsRepository.GetByTenantIdAsync(request.TenantId);
        var patientSettings = await patientAlertSettingsRepository.GetByPatientIdAsync(request.PatientId);

        return AlertEvaluationService.ResolvePatientAlertSettings(patient, clinicSettings, patientSettings);
    }
}
