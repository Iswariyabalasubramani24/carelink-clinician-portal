using CareLink.Application.Auth.Commands;
using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using Moq;

namespace CareLink.UnitTests.Auth;

public class ChangePasswordCommandHandlerTests
{
    private static Clinician MakeClinician() => new()
    {
        Id = 1,
        TenantId = 1,
        Email = "doctor@apollo.com",
        PasswordHash = "old-hash",
        FirstName = "Anita",
        LastName = "Rao",
        Role = ClinicianRole.Clinician,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    private static ChangePasswordCommand ValidCommand() => new()
    {
        ClinicianId = 1,
        TenantId = 1,
        CurrentPassword = "Temp#Passw0rd",
        NewPassword = "MyNew#Passw0rd"
    };

    [Fact]
    public async Task Handle_CorrectCurrentPassword_UpdatesHashAndAudits()
    {
        var clinician = MakeClinician();

        var clinicianRepo = new Mock<IClinicianRepository>();
        clinicianRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(clinician);

        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(h => h.Verify("Temp#Passw0rd", "old-hash")).Returns(true);
        hasher.Setup(h => h.Hash("MyNew#Passw0rd")).Returns("new-hash");

        var auditLogger = new Mock<IAuditLogger>();

        var handler = new ChangePasswordCommandHandler(clinicianRepo.Object, hasher.Object, auditLogger.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal("new-hash", clinician.PasswordHash);
        clinicianRepo.Verify(r => r.UpdateAsync(clinician), Times.Once);
        auditLogger.Verify(
            a => a.LogAsync(1, 1, "PasswordChanged", "Clinician", 1, null),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WrongCurrentPassword_ThrowsWithoutChangingAnything()
    {
        var clinician = MakeClinician();

        var clinicianRepo = new Mock<IClinicianRepository>();
        clinicianRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(clinician);

        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        var handler = new ChangePasswordCommandHandler(
            clinicianRepo.Object, hasher.Object, Mock.Of<IAuditLogger>());

        await Assert.ThrowsAsync<InvalidCurrentPasswordException>(() =>
            handler.Handle(ValidCommand(), CancellationToken.None));

        Assert.Equal("old-hash", clinician.PasswordHash);
        clinicianRepo.Verify(r => r.UpdateAsync(It.IsAny<Clinician>()), Times.Never);
    }

    [Fact]
    public async Task Handle_TooShortNewPassword_IsRejectedBeforeAnyLookup()
    {
        var clinicianRepo = new Mock<IClinicianRepository>();

        var handler = new ChangePasswordCommandHandler(
            clinicianRepo.Object, Mock.Of<IPasswordHasher>(), Mock.Of<IAuditLogger>());

        var command = ValidCommand();
        command.NewPassword = "short";

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(command, CancellationToken.None));

        clinicianRepo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }
}
