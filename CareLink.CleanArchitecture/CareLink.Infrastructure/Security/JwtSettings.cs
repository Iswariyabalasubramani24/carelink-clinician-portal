namespace CareLink.Infrastructure.Security;

public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public int AccessTokenExpiryMinutes { get; set; } = 15;

    // Sliding idle window: each refresh rotates the refresh token and extends
    // it by this much. A clinician inactive for longer (or returning after
    // closing the browser beyond it) must sign in again - clinical-app
    // session hygiene rather than a long-lived "remember me".
    public int RefreshTokenIdleTimeoutMinutes { get; set; } = 30;
}
