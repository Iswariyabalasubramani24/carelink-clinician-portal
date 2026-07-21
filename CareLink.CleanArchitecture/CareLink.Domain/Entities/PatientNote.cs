namespace CareLink.Domain.Entities;

public class PatientNote
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public int TenantId { get; set; }

    public int ClinicianId { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public Patient Patient { get; set; } = null!;
}
