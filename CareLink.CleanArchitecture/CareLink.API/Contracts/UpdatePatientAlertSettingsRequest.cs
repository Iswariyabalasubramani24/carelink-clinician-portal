using CareLink.Application.Alerts;

namespace CareLink.API.Contracts;

public record UpdatePatientAlertSettingsRequest(bool UseOverride, List<PatientAlertOverrideInput> Overrides);
