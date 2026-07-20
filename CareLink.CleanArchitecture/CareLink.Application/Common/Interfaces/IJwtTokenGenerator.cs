using CareLink.Domain.Entities;

namespace CareLink.Application.Common.Interfaces;

public record AccessTokenResult(string Token, DateTime ExpiresAt);

public record RefreshTokenResult(string Token, DateTime ExpiresAt);

public interface IJwtTokenGenerator
{
    AccessTokenResult GenerateAccessToken(Clinician clinician, int tenantId);

    RefreshTokenResult GenerateRefreshToken();
}
