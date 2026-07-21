using CareLink.Application.ClinicUsers.Commands;
using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using Moq;

namespace CareLink.UnitTests.ClinicUsers;

// Note: [Authorize(Roles="Admin")] on ClinicUsersController is what actually
// rejects non-admins with 403 - that's ASP.NET Core's authorization
// middleware, not handler logic, so it isn't re-verified here. What IS
// verified at this layer: CreateClinicUserCommand.TenantId is the only
// source of the created user's tenant - the API contract (CreateClinicUserRequest)
// has no TenantId field at all, so a malicious client body can never override
// the tenantId the controller derives from the JWT "tenantId" claim.
public class CreateClinicUserCommandHandlerTests
{
    private static CreateClinicUserCommand ValidCommand() => new()
    {
        TenantId = 1,
        FirstName = "Priya",
        LastName = "Nair",
        Email = "priya.nair@apollo.com",
        LanguageCode = "en",
        Role = ClinicianRole.Clinician
    };

    [Fact]
    public async Task Handle_ValidRequest_CreatesUserInRequestedTenantAndReturnsPlaintextTempPassword()
    {
        var clinicianRepo = new Mock<IClinicianRepository>();
        clinicianRepo.Setup(r => r.GetByEmailAsync("priya.nair@apollo.com")).ReturnsAsync((Clinician?)null);

        Clinician? captured = null;
        clinicianRepo.Setup(r => r.AddAsync(It.IsAny<Clinician>()))
            .Callback<Clinician>(c => { c.Id = 42; captured = c; })
            .ReturnsAsync((Clinician c) => c);

        var tempPasswordGenerator = new Mock<ITemporaryPasswordGenerator>();
        tempPasswordGenerator.Setup(g => g.Generate()).Returns("Tmp#Passw0rd");

        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher.Setup(h => h.Hash("Tmp#Passw0rd")).Returns("hashed-temp-password");

        var handler = new CreateClinicUserCommandHandler(clinicianRepo.Object, tempPasswordGenerator.Object, passwordHasher.Object, new Mock<IAuditLogger>().Object);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal("Tmp#Passw0rd", result.TemporaryPassword);
        Assert.Equal(42, result.User.Id);
        Assert.Equal("Priya", result.User.FirstName);
        Assert.Equal("Clinician", result.User.Role);
        Assert.True(result.User.IsActive);

        Assert.NotNull(captured);
        Assert.Equal(1, captured!.TenantId);
        Assert.Equal("hashed-temp-password", captured.PasswordHash);
        Assert.NotEqual("Tmp#Passw0rd", captured.PasswordHash);
    }

    [Fact]
    public async Task Handle_EmailAlreadyExists_ThrowsEmailAlreadyInUseException()
    {
        var existing = new Clinician { Id = 1, TenantId = 1, Email = "priya.nair@apollo.com" };

        var clinicianRepo = new Mock<IClinicianRepository>();
        clinicianRepo.Setup(r => r.GetByEmailAsync("priya.nair@apollo.com")).ReturnsAsync(existing);

        var tempPasswordGenerator = new Mock<ITemporaryPasswordGenerator>();
        var passwordHasher = new Mock<IPasswordHasher>();

        var handler = new CreateClinicUserCommandHandler(clinicianRepo.Object, tempPasswordGenerator.Object, passwordHasher.Object, new Mock<IAuditLogger>().Object);

        await Assert.ThrowsAsync<EmailAlreadyInUseException>(
            () => handler.Handle(ValidCommand(), CancellationToken.None));

        clinicianRepo.Verify(r => r.AddAsync(It.IsAny<Clinician>()), Times.Never);
    }
}
