namespace CareLink.Domain.Entities;

public class Clinician
{
    public int Id { get; set; }

    public int TenantId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public ClinicianRole Role { get; set; }

    public string LanguageCode { get; set; } = "en";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
}
