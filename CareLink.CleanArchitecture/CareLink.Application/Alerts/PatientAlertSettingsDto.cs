using CareLink.Domain.Entities;

namespace CareLink.Application.Alerts;

public record PatientAlertSettingsDto(AlertType AlertType, AlertUrgency EffectiveUrgency, bool IsOverride);
