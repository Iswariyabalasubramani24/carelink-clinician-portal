using CareLink.Application.Auth;

namespace CareLink.API.Contracts;

public record LoginResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    int ClinicianId,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    int TenantId)
{
    public static LoginResponse FromAuthResult(AuthResultDto result) => new(
        result.AccessToken,
        result.AccessTokenExpiresAt,
        result.ClinicianId,
        result.Email,
        result.FirstName,
        result.LastName,
        result.Role,
        result.TenantId);
}
