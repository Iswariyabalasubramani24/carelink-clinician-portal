namespace CareLink.Domain.Entities;

public class Alert
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public int TenantId { get; set; }

    public AlertType AlertType { get; set; }

    public AlertUrgency Urgency { get; set; }

    public DateTime TriggeredAt { get; set; }

    public bool IsAcknowledged { get; set; }

    public DateTime? AcknowledgedAt { get; set; }

    // Temporarily suppresses the alert from the "active" list without
    // permanently dismissing it - re-surfaces once this passes.
    public DateTime? SnoozedUntil { get; set; }

    public Patient Patient { get; set; } = null!;
}
