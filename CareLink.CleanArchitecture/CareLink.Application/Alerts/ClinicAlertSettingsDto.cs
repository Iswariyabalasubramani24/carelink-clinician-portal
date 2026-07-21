using CareLink.Domain.Entities;

namespace CareLink.Application.Alerts;

public record ClinicAlertSettingsDto(AlertType AlertType, AlertUrgency DefaultUrgency);
