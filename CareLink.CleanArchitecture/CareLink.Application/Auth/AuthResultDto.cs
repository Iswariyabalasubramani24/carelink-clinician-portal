namespace CareLink.Application.Auth;

public record AuthResultDto(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    int ClinicianId,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    int TenantId);
