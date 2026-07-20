using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using MediatR;

namespace CareLink.Application.Auth.Commands;

public record SwitchTenantResult(string AccessToken, DateTime AccessTokenExpiresAt, int TenantId);

public class SwitchTenantCommand : IRequest<SwitchTenantResult>
{
    // Set server-side from the authenticated access token's claims - never
    // trust a client-supplied clinician id.
    public int ClinicianId { get; set; }

    // Set server-side from the httpOnly refresh-token cookie.
    public string RefreshToken { get; set; } = string.Empty;

    // The only client-supplied value: which hospital to switch to.
    public int TenantId { get; set; }
}

public class SwitchTenantCommandHandler(
    IClinicianTenantRepository clinicianTenantRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IJwtTokenGenerator tokenGenerator) : IRequestHandler<SwitchTenantCommand, SwitchTenantResult>
{
    public async Task<SwitchTenantResult> Handle(SwitchTenantCommand request, CancellationToken cancellationToken)
    {
        var hasAccess = await clinicianTenantRepository.HasAccessAsync(request.ClinicianId, request.TenantId);
        if (!hasAccess)
        {
            throw new TenantAccessDeniedException();
        }

        var storedToken = await refreshTokenRepository.GetByTokenAsync(request.RefreshToken);
        if (storedToken is null || storedToken.IsRevoked || storedToken.ExpiresAt < DateTime.UtcNow
            || storedToken.ClinicianId != request.ClinicianId)
        {
            throw new InvalidRefreshTokenException();
        }

        await refreshTokenRepository.UpdateActiveTenantAsync(storedToken, request.TenantId);

        var accessToken = tokenGenerator.GenerateAccessToken(storedToken.Clinician, request.TenantId);

        return new SwitchTenantResult(accessToken.Token, accessToken.ExpiresAt, request.TenantId);
    }
}
