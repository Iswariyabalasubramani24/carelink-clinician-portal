using CareLink.Application.Auth.Commands;
using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using Moq;

namespace CareLink.UnitTests.Auth;

public class SwitchTenantCommandHandlerTests
{
    private static Clinician MakeClinician() => new()
    {
        Id = 1,
        TenantId = 1,
        Email = "doctor@apollo.com",
        PasswordHash = "hashed-password",
        FirstName = "Anita",
        LastName = "Rao",
        Role = ClinicianRole.Clinician,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    private static RefreshToken MakeStoredToken(Clinician clinician, int activeTenantId) => new()
    {
        Id = 10,
        ClinicianId = clinician.Id,
        TenantId = activeTenantId,
        Token = "refresh-token-value",
        ExpiresAt = DateTime.UtcNow.AddDays(7),
        IsRevoked = false,
        Clinician = clinician
    };

    [Fact]
    public async Task Handle_ValidSwitch_ReturnsNewAccessTokenAndUpdatesActiveTenant()
    {
        var clinician = MakeClinician();
        var storedToken = MakeStoredToken(clinician, activeTenantId: 1);
        var accessToken = new AccessTokenResult("new-access-token", DateTime.UtcNow.AddMinutes(15));

        var clinicianTenantRepo = new Mock<IClinicianTenantRepository>();
        clinicianTenantRepo.Setup(r => r.HasAccessAsync(1, 2)).ReturnsAsync(true);

        var refreshTokenRepo = new Mock<IRefreshTokenRepository>();
        refreshTokenRepo.Setup(r => r.GetByTokenAsync("refresh-token-value")).ReturnsAsync(storedToken);

        var tokenGenerator = new Mock<IJwtTokenGenerator>();
        tokenGenerator.Setup(g => g.GenerateAccessToken(clinician, 2)).Returns(accessToken);

        var handler = new SwitchTenantCommandHandler(
            clinicianTenantRepo.Object, refreshTokenRepo.Object, tokenGenerator.Object);

        var result = await handler.Handle(
            new SwitchTenantCommand { ClinicianId = 1, RefreshToken = "refresh-token-value", TenantId = 2 },
            CancellationToken.None);

        Assert.Equal("new-access-token", result.AccessToken);
        Assert.Equal(2, result.TenantId);

        refreshTokenRepo.Verify(r => r.UpdateActiveTenantAsync(storedToken, 2), Times.Once);
    }

    [Fact]
    public async Task Handle_ClinicianLacksAccessToRequestedTenant_ThrowsTenantAccessDenied()
    {
        var clinicianTenantRepo = new Mock<IClinicianTenantRepository>();
        clinicianTenantRepo.Setup(r => r.HasAccessAsync(1, 3)).ReturnsAsync(false);

        var refreshTokenRepo = new Mock<IRefreshTokenRepository>();
        var tokenGenerator = new Mock<IJwtTokenGenerator>();

        var handler = new SwitchTenantCommandHandler(
            clinicianTenantRepo.Object, refreshTokenRepo.Object, tokenGenerator.Object);

        await Assert.ThrowsAsync<TenantAccessDeniedException>(
            () => handler.Handle(
                new SwitchTenantCommand { ClinicianId = 1, RefreshToken = "refresh-token-value", TenantId = 3 },
                CancellationToken.None));

        refreshTokenRepo.Verify(r => r.GetByTokenAsync(It.IsAny<string>()), Times.Never);
        refreshTokenRepo.Verify(r => r.UpdateActiveTenantAsync(It.IsAny<RefreshToken>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RevokedRefreshToken_ThrowsInvalidRefreshTokenException()
    {
        var clinician = MakeClinician();
        var storedToken = MakeStoredToken(clinician, activeTenantId: 1);
        storedToken.IsRevoked = true;

        var clinicianTenantRepo = new Mock<IClinicianTenantRepository>();
        clinicianTenantRepo.Setup(r => r.HasAccessAsync(1, 2)).ReturnsAsync(true);

        var refreshTokenRepo = new Mock<IRefreshTokenRepository>();
        refreshTokenRepo.Setup(r => r.GetByTokenAsync("refresh-token-value")).ReturnsAsync(storedToken);

        var tokenGenerator = new Mock<IJwtTokenGenerator>();

        var handler = new SwitchTenantCommandHandler(
            clinicianTenantRepo.Object, refreshTokenRepo.Object, tokenGenerator.Object);

        await Assert.ThrowsAsync<InvalidRefreshTokenException>(
            () => handler.Handle(
                new SwitchTenantCommand { ClinicianId = 1, RefreshToken = "refresh-token-value", TenantId = 2 },
                CancellationToken.None));
    }
}
