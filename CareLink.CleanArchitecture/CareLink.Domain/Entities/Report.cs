namespace CareLink.Domain.Entities;

public class Report
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public int TenantId { get; set; }

    public ReportType ReportType { get; set; }

    public DateTime GeneratedAt { get; set; }

    // JSON snapshot of the patient/equipment/transmission/alert data used to
    // build the PDF - captured once at generation time so re-downloading
    // later reproduces the same content even if the patient's live data has
    // since changed.
    public string DataSnapshot { get; set; } = string.Empty;

    public Patient Patient { get; set; } = null!;
}
