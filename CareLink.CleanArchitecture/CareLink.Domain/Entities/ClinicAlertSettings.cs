namespace CareLink.Domain.Entities;

// Clinic-wide (tenant-wide) default urgency for each alert type. A
// PatientAlertSettings row with IsOverride=true takes precedence over this
// for that specific patient/type.
public class ClinicAlertSettings
{
    public int Id { get; set; }

    public int TenantId { get; set; }

    public AlertType AlertType { get; set; }

    public AlertUrgency DefaultUrgency { get; set; }

    public Tenant Tenant { get; set; } = null!;
}
