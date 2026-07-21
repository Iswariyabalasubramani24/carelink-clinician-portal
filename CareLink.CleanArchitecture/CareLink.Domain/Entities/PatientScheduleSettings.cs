namespace CareLink.Domain.Entities;

// Mirrors the ReportSettings clinic-default + per-patient-override pattern:
// a row with TenantId set is a clinic-wide default; a row with PatientId set
// is a per-patient override. Exactly one of the two is populated per row.
public class PatientScheduleSettings
{
    public int Id { get; set; }

    public int? TenantId { get; set; }

    public int? PatientId { get; set; }

    public int IntervalDays { get; set; }

    public Tenant? Tenant { get; set; }

    public Patient? Patient { get; set; }
}
