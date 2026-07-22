using CareLink.Application.Auth;
using CareLink.Application.Auth.Commands;

namespace CareLink.API.Contracts;

// Shared response shape for login and refresh. Deliberately excludes the
// refresh token, which travels only in the httpOnly cookie.
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

    public static LoginResponse FromRefreshResult(RefreshAccessTokenResult result) => new(
        result.AccessToken,
        result.AccessTokenExpiresAt,
        result.ClinicianId,
        result.Email,
        result.FirstName,
        result.LastName,
        result.Role,
        result.TenantId);
}
