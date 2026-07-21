using CareLink.Application.ClinicUsers.Commands;
using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using Moq;

namespace CareLink.UnitTests.ClinicUsers;

public class SuspendActivateClinicUserCommandHandlerTests
{
    private static Clinician MakeClinician(int id, int tenantId, bool isActive) => new()
    {
        Id = id,
        TenantId = tenantId,
        FirstName = "Priya",
        LastName = "Nair",
        Email = "priya.nair@apollo.com",
        Role = ClinicianRole.Clinician,
        LanguageCode = "en",
        IsActive = isActive,
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task Suspend_ValidUser_SetsIsActiveFalse()
    {
        var clinician = MakeClinician(1, tenantId: 1, isActive: true);

        var clinicianRepo = new Mock<IClinicianRepository>();
        clinicianRepo.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(clinician);

        var handler = new SuspendClinicUserCommandHandler(clinicianRepo.Object);

        var result = await handler.Handle(new SuspendClinicUserCommand(1, 1), CancellationToken.None);

        Assert.False(result.IsActive);
        clinicianRepo.Verify(r => r.UpdateAsync(It.Is<Clinician>(c => !c.IsActive)), Times.Once);
    }

    [Fact]
    public async Task Suspend_UserBelongsToDifferentTenant_ThrowsClinicianNotFoundException()
    {
        // The lookup is tenant-scoped, so an admin can never suspend a user
        // belonging to a different hospital.
        var clinicianRepo = new Mock<IClinicianRepository>();
        clinicianRepo.Setup(r => r.GetByIdAsync(1, 2)).ReturnsAsync((Clinician?)null);

        var handler = new SuspendClinicUserCommandHandler(clinicianRepo.Object);

        await Assert.ThrowsAsync<ClinicianNotFoundException>(
            () => handler.Handle(new SuspendClinicUserCommand(1, 2), CancellationToken.None));

        clinicianRepo.Verify(r => r.UpdateAsync(It.IsAny<Clinician>()), Times.Never);
    }

    [Fact]
    public async Task Activate_ValidUser_SetsIsActiveTrue()
    {
        var clinician = MakeClinician(1, tenantId: 1, isActive: false);

        var clinicianRepo = new Mock<IClinicianRepository>();
        clinicianRepo.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(clinician);

        var handler = new ActivateClinicUserCommandHandler(clinicianRepo.Object);

        var result = await handler.Handle(new ActivateClinicUserCommand(1, 1), CancellationToken.None);

        Assert.True(result.IsActive);
        clinicianRepo.Verify(r => r.UpdateAsync(It.Is<Clinician>(c => c.IsActive)), Times.Once);
    }

    [Fact]
    public async Task Activate_UserBelongsToDifferentTenant_ThrowsClinicianNotFoundException()
    {
        var clinicianRepo = new Mock<IClinicianRepository>();
        clinicianRepo.Setup(r => r.GetByIdAsync(1, 2)).ReturnsAsync((Clinician?)null);

        var handler = new ActivateClinicUserCommandHandler(clinicianRepo.Object);

        await Assert.ThrowsAsync<ClinicianNotFoundException>(
            () => handler.Handle(new ActivateClinicUserCommand(1, 2), CancellationToken.None));

        clinicianRepo.Verify(r => r.UpdateAsync(It.IsAny<Clinician>()), Times.Never);
    }
}
