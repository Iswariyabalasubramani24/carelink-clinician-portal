using CareLink.Domain.Entities;

namespace CareLink.Application.Common.Interfaces;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken refreshToken);

    Task<RefreshToken?> GetByTokenAsync(string token);

    Task RevokeAsync(RefreshToken refreshToken);

    Task UpdateActiveTenantAsync(RefreshToken refreshToken, int tenantId);
}
