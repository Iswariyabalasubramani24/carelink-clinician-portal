using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using MediatR;

namespace CareLink.Application.Auth.Commands;

public record RefreshAccessTokenResult(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    int ClinicianId,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    int TenantId);

public class RefreshTokenCommand : IRequest<RefreshAccessTokenResult>
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class RefreshTokenCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IJwtTokenGenerator tokenGenerator) : IRequestHandler<RefreshTokenCommand, RefreshAccessTokenResult>
{
    public async Task<RefreshAccessTokenResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var storedToken = await refreshTokenRepository.GetByTokenAsync(request.RefreshToken);

        if (storedToken is null || storedToken.IsRevoked || storedToken.ExpiresAt < DateTime.UtcNow)
        {
            throw new InvalidRefreshTokenException();
        }

        var clinician = storedToken.Clinician;
        var accessToken = tokenGenerator.GenerateAccessToken(clinician);

        return new RefreshAccessTokenResult(
            accessToken.Token,
            accessToken.ExpiresAt,
            clinician.Id,
            clinician.Email,
            clinician.FirstName,
            clinician.LastName,
            clinician.Role.ToString(),
            clinician.TenantId);
    }
}
