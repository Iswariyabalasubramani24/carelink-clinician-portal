namespace CareLink.Domain.Entities;

// Mirrors the ClinicAlertSettings/PatientAlertSettings pattern, but since
// there's only one scalar value (no per-type variants), it's a single table:
// a row with TenantId set is a clinic-wide default; a row with PatientId set
// is a per-patient override. Exactly one of the two is populated per row.
public class ReportSettings
{
    public int Id { get; set; }

    public int? TenantId { get; set; }

    public int? PatientId { get; set; }

    public int IntervalDays { get; set; }

    public Tenant? Tenant { get; set; }

    public Patient? Patient { get; set; }
}
