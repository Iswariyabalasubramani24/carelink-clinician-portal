namespace CareLink.Domain.Entities;

public class RefreshToken
{
    public int Id { get; set; }

    public int ClinicianId { get; set; }

    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public bool IsRevoked { get; set; }

    public Clinician Clinician { get; set; } = null!;
}
