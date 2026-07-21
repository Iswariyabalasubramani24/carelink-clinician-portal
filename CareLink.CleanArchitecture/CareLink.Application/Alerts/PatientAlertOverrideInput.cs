using CareLink.Domain.Entities;

namespace CareLink.Application.Alerts;

public record PatientAlertOverrideInput(AlertType AlertType, AlertUrgency Urgency);
