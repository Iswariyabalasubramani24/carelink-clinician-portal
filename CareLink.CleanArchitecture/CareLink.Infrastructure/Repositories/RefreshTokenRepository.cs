using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CareLink.Infrastructure.Repositories;

public class RefreshTokenRepository(ApplicationDbContext db) : IRefreshTokenRepository
{
    public async Task AddAsync(RefreshToken refreshToken)
    {
        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync();
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        return await db.RefreshTokens
            .Include(r => r.Clinician)
            .FirstOrDefaultAsync(r => r.Token == token);
    }

    public async Task RevokeAsync(RefreshToken refreshToken)
    {
        refreshToken.IsRevoked = true;
        await db.SaveChangesAsync();
    }
}
