using CareLink.Domain.Entities;

namespace CareLink.Application.Alerts;

public record AlertDto(
    int Id,
    int PatientId,
    int TenantId,
    AlertType AlertType,
    AlertUrgency Urgency,
    DateTime TriggeredAt,
    bool IsAcknowledged,
    DateTime? AcknowledgedAt,
    DateTime? SnoozedUntil)
{
    public static AlertDto FromEntity(Alert a) => new(
        a.Id,
        a.PatientId,
        a.TenantId,
        a.AlertType,
        a.Urgency,
        a.TriggeredAt,
        a.IsAcknowledged,
        a.AcknowledgedAt,
        a.SnoozedUntil);
}
