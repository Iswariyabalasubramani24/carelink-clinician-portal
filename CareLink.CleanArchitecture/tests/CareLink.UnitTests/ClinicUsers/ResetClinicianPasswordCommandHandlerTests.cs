using CareLink.Application.ClinicUsers.Commands;
using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using Moq;

namespace CareLink.UnitTests.ClinicUsers;

public class ResetClinicianPasswordCommandHandlerTests
{
    private static Clinician MakeClinician(int id, int tenantId) => new()
    {
        Id = id,
        TenantId = tenantId,
        FirstName = "Priya",
        LastName = "Nair",
        Email = "priya.nair@apollo.com",
        PasswordHash = "old-hash",
        Role = ClinicianRole.Admin,
        LanguageCode = "en",
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    private static ResetClinicianPasswordCommand Command(int clinicianId, int tenantId, int actingClinicianId = 2) => new()
    {
        ClinicianId = clinicianId,
        TenantId = tenantId,
        ActingClinicianId = actingClinicianId
    };

    [Fact]
    public async Task Handle_ValidUser_RehashesAndReturnsTemporaryPassword()
    {
        var clinician = MakeClinician(1, tenantId: 1);

        var clinicianRepo = new Mock<IClinicianRepository>();
        clinicianRepo.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(clinician);

        var tempPasswordGenerator = new Mock<ITemporaryPasswordGenerator>();
        tempPasswordGenerator.Setup(g => g.Generate()).Returns("Tmp#Passw0rd1");

        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(h => h.Hash("Tmp#Passw0rd1")).Returns("new-hash");

        var auditLogger = new Mock<IAuditLogger>();

        var handler = new ResetClinicianPasswordCommandHandler(
            clinicianRepo.Object, tempPasswordGenerator.Object, hasher.Object, auditLogger.Object);

        var result = await handler.Handle(Command(1, 1), CancellationToken.None);

        Assert.Equal("Tmp#Passw0rd1", result.TemporaryPassword);
        Assert.Equal(clinician.Email, result.User.Email);
        Assert.Equal("new-hash", clinician.PasswordHash);
        clinicianRepo.Verify(r => r.UpdateAsync(clinician), Times.Once);
        auditLogger.Verify(
            a => a.LogAsync(2, 1, "PasswordReset", "Clinician", 1, clinician.Email),
            Times.Once);
    }

    [Fact]
    public async Task Handle_UserBelongsToDifferentTenant_ThrowsClinicianNotFoundException()
    {
        // The lookup is tenant-scoped, so an admin can never reset the
        // password of a user belonging to a different hospital.
        var clinicianRepo = new Mock<IClinicianRepository>();
        clinicianRepo.Setup(r => r.GetByIdAsync(1, 2)).ReturnsAsync((Clinician?)null);

        var handler = new ResetClinicianPasswordCommandHandler(
            clinicianRepo.Object, Mock.Of<ITemporaryPasswordGenerator>(), Mock.Of<IPasswordHasher>(), Mock.Of<IAuditLogger>());

        await Assert.ThrowsAsync<ClinicianNotFoundException>(
            () => handler.Handle(Command(1, 2), CancellationToken.None));

        clinicianRepo.Verify(r => r.UpdateAsync(It.IsAny<Clinician>()), Times.Never);
    }
}
