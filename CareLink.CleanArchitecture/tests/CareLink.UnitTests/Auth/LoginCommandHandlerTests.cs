using CareLink.Application.Auth.Commands;
using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using Moq;

namespace CareLink.UnitTests.Auth;

public class LoginCommandHandlerTests
{
    private static Clinician MakeClinician(bool isActive = true) => new()
    {
        Id = 1,
        TenantId = 1,
        Email = "doctor@apollo.com",
        PasswordHash = "hashed-password",
        FirstName = "Anita",
        LastName = "Rao",
        Role = ClinicianRole.Clinician,
        IsActive = isActive,
        CreatedAt = DateTime.UtcNow
    };

    private static LoginCommand ValidCommand() => new()
    {
        Email = "doctor@apollo.com",
        Password = "Test@123"
    };

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsAuthResultAndPersistsRefreshToken()
    {
        var clinician = MakeClinician();
        var accessToken = new AccessTokenResult("access-token-value", DateTime.UtcNow.AddMinutes(15));
        var refreshToken = new RefreshTokenResult("refresh-token-value", DateTime.UtcNow.AddDays(7));

        var clinicianRepo = new Mock<IClinicianRepository>();
        clinicianRepo.Setup(r => r.GetByEmailAsync("doctor@apollo.com")).ReturnsAsync(clinician);

        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher.Setup(h => h.Verify("Test@123", "hashed-password")).Returns(true);

        var tokenGenerator = new Mock<IJwtTokenGenerator>();
        tokenGenerator.Setup(g => g.GenerateAccessToken(clinician)).Returns(accessToken);
        tokenGenerator.Setup(g => g.GenerateRefreshToken()).Returns(refreshToken);

        var refreshTokenRepo = new Mock<IRefreshTokenRepository>();

        var handler = new LoginCommandHandler(
            clinicianRepo.Object, passwordHasher.Object, tokenGenerator.Object, refreshTokenRepo.Object);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal("access-token-value", result.AccessToken);
        Assert.Equal("refresh-token-value", result.RefreshToken);
        Assert.Equal(1, result.ClinicianId);
        Assert.Equal(1, result.TenantId);
        Assert.Equal("doctor@apollo.com", result.Email);
        Assert.Equal("Anita", result.FirstName);
        Assert.Equal("Rao", result.LastName);
        Assert.Equal("Clinician", result.Role);

        refreshTokenRepo.Verify(
            r => r.AddAsync(It.Is<RefreshToken>(t =>
                t.ClinicianId == 1 && t.Token == "refresh-token-value" && !t.IsRevoked)),
            Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidPassword_ThrowsInvalidCredentialsException()
    {
        var clinician = MakeClinician();

        var clinicianRepo = new Mock<IClinicianRepository>();
        clinicianRepo.Setup(r => r.GetByEmailAsync("doctor@apollo.com")).ReturnsAsync(clinician);

        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        var tokenGenerator = new Mock<IJwtTokenGenerator>();
        var refreshTokenRepo = new Mock<IRefreshTokenRepository>();

        var handler = new LoginCommandHandler(
            clinicianRepo.Object, passwordHasher.Object, tokenGenerator.Object, refreshTokenRepo.Object);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => handler.Handle(new LoginCommand { Email = "doctor@apollo.com", Password = "WrongPassword" }, CancellationToken.None));

        refreshTokenRepo.Verify(r => r.AddAsync(It.IsAny<RefreshToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NonExistentEmail_ThrowsInvalidCredentialsException()
    {
        var clinicianRepo = new Mock<IClinicianRepository>();
        clinicianRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((Clinician?)null);

        var passwordHasher = new Mock<IPasswordHasher>();
        var tokenGenerator = new Mock<IJwtTokenGenerator>();
        var refreshTokenRepo = new Mock<IRefreshTokenRepository>();

        var handler = new LoginCommandHandler(
            clinicianRepo.Object, passwordHasher.Object, tokenGenerator.Object, refreshTokenRepo.Object);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => handler.Handle(new LoginCommand { Email = "nobody@apollo.com", Password = "Test@123" }, CancellationToken.None));

        passwordHasher.Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        refreshTokenRepo.Verify(r => r.AddAsync(It.IsAny<RefreshToken>()), Times.Never);
    }
}
