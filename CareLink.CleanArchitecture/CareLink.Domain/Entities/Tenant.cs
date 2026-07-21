namespace CareLink.Domain.Entities;

public class Tenant
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Region { get; set; } = string.Empty;

    public string LanguageCode { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    // Marks the single hidden "platform" tenant that anchors SuperAdmin accounts.
    // Excluded from the public tenant list, the clinic switcher, and the
    // super-admin hospital-management list - it is infrastructure, not a hospital.
    public bool IsSystem { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<Patient> Patients { get; set; } = new List<Patient>();
}
