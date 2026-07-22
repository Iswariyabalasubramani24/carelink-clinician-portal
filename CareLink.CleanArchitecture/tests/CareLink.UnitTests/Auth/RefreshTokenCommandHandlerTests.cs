using CareLink.Application.Auth.Commands;
using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using Moq;

namespace CareLink.UnitTests.Auth;

public class RefreshTokenCommandHandlerTests
{
    private static Clinician MakeClinician() => new()
    {
        Id = 1,
        TenantId = 1,
        Email = "doctor@apollo.com",
        PasswordHash = "irrelevant",
        FirstName = "Anita",
        LastName = "Rao",
        Role = ClinicianRole.Clinician,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    private static RefreshToken MakeStoredToken(Clinician clinician, int tenantId = 2) => new()
    {
        Id = 10,
        ClinicianId = clinician.Id,
        // TenantId deliberately differs from the clinician's default tenant to
        // prove the active-tenant selection survives a refresh.
        TenantId = tenantId,
        Token = "current-refresh-token",
        ExpiresAt = DateTime.UtcNow.AddMinutes(20),
        IsRevoked = false,
        Clinician = clinician
    };

    [Fact]
    public async Task Handle_ValidToken_RotatesTheRefreshToken()
    {
        var clinician = MakeClinician();
        var stored = MakeStoredToken(clinician);

        var refreshTokenRepo = new Mock<IRefreshTokenRepository>();
        refreshTokenRepo.Setup(r => r.GetByTokenAsync("current-refresh-token")).ReturnsAsync(stored);

        var tokenGenerator = new Mock<IJwtTokenGenerator>();
        tokenGenerator.Setup(g => g.GenerateAccessToken(clinician, stored.TenantId))
            .Returns(new AccessTokenResult("new-access-token", DateTime.UtcNow.AddMinutes(15)));
        var rotatedExpiry = DateTime.UtcNow.AddMinutes(30);
        tokenGenerator.Setup(g => g.GenerateRefreshToken())
            .Returns(new RefreshTokenResult("rotated-refresh-token", rotatedExpiry));

        var handler = new RefreshTokenCommandHandler(refreshTokenRepo.Object, tokenGenerator.Object);

        var result = await handler.Handle(
            new RefreshTokenCommand { RefreshToken = "current-refresh-token" }, CancellationToken.None);

        Assert.Equal("new-access-token", result.AccessToken);
        Assert.Equal("rotated-refresh-token", result.RefreshToken);
        Assert.Equal(rotatedExpiry, result.RefreshTokenExpiresAt);
        Assert.Equal(stored.TenantId, result.TenantId);

        // The presented token must be dead after use, and the replacement
        // stored against the same clinician and active tenant.
        refreshTokenRepo.Verify(r => r.RevokeAsync(stored), Times.Once);
        refreshTokenRepo.Verify(
            r => r.AddAsync(It.Is<RefreshToken>(t =>
                t.ClinicianId == clinician.Id
                && t.TenantId == stored.TenantId
                && t.Token == "rotated-refresh-token"
                && !t.IsRevoked)),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ExpiredToken_ThrowsInvalidRefreshTokenException()
    {
        var clinician = MakeClinician();
        var stored = MakeStoredToken(clinician);
        stored.ExpiresAt = DateTime.UtcNow.AddMinutes(-1); // idle window elapsed

        var refreshTokenRepo = new Mock<IRefreshTokenRepository>();
        refreshTokenRepo.Setup(r => r.GetByTokenAsync("current-refresh-token")).ReturnsAsync(stored);

        var handler = new RefreshTokenCommandHandler(refreshTokenRepo.Object, Mock.Of<IJwtTokenGenerator>());

        await Assert.ThrowsAsync<InvalidRefreshTokenException>(() =>
            handler.Handle(new RefreshTokenCommand { RefreshToken = "current-refresh-token" }, CancellationToken.None));

        refreshTokenRepo.Verify(r => r.AddAsync(It.IsAny<RefreshToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyRotatedToken_IsRejected()
    {
        // A replayed (already-revoked) token must not mint a new session -
        // this is the security property rotation buys us.
        var clinician = MakeClinician();
        var stored = MakeStoredToken(clinician);
        stored.IsRevoked = true;

        var refreshTokenRepo = new Mock<IRefreshTokenRepository>();
        refreshTokenRepo.Setup(r => r.GetByTokenAsync("current-refresh-token")).ReturnsAsync(stored);

        var handler = new RefreshTokenCommandHandler(refreshTokenRepo.Object, Mock.Of<IJwtTokenGenerator>());

        await Assert.ThrowsAsync<InvalidRefreshTokenException>(() =>
            handler.Handle(new RefreshTokenCommand { RefreshToken = "current-refresh-token" }, CancellationToken.None));
    }
}
