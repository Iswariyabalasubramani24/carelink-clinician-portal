namespace CareLink.Domain.Entities;

// A per-patient override of the clinic's default urgency for one alert type.
// Absence of a row (or IsOverride=false) means the clinic default applies.
public class PatientAlertSettings
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public AlertType AlertType { get; set; }

    public AlertUrgency Urgency { get; set; }

    public bool IsOverride { get; set; }

    public Patient Patient { get; set; } = null!;
}
