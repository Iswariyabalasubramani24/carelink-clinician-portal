namespace CareLink.Domain.Entities;

public class RefreshToken
{
    public int Id { get; set; }

    public int ClinicianId { get; set; }

    // The tenant that was active when this refresh token was issued (or most
    // recently switched to). A silent access-token refresh must reissue the
    // token for THIS tenant, not the clinician's default - otherwise switching
    // hospitals would silently revert on the next refresh.
    public int TenantId { get; set; }

    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public bool IsRevoked { get; set; }

    public Clinician Clinician { get; set; } = null!;
}
