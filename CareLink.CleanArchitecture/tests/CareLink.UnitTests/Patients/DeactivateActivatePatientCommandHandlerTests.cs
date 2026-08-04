using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using CareLink.Application.Patients.Commands;
using CareLink.Domain.Entities;
using Moq;

namespace CareLink.UnitTests.Patients;

public class DeactivateActivatePatientCommandHandlerTests
{
    private static Patient MakePatient(int id, int tenantId, bool isActive) => new()
    {
        Id = id,
        TenantId = tenantId,
        MedicalRecordNumber = "APL-1001",
        FirstName = "Rajesh",
        LastName = "Kumar",
        DateOfBirth = new DateTime(1965, 4, 12),
        DeviceType = DeviceType.ICD,
        DeviceSerialNumber = "MDT-ICD-0001",
        ImplantDate = new DateTime(2022, 3, 15),
        IsActive = isActive,
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task Deactivate_ValidPatient_SetsIsActiveFalse()
    {
        var patient = MakePatient(1, tenantId: 1, isActive: true);

        var repositoryMock = new Mock<IPatientRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(patient);

        var handler = new DeactivatePatientCommandHandler(repositoryMock.Object, Mock.Of<IAuditLogger>());

        var result = await handler.Handle(new DeactivatePatientCommand(1, 1), CancellationToken.None);

        Assert.False(result.IsActive);
        repositoryMock.Verify(r => r.UpdateAsync(It.Is<Patient>(p => !p.IsActive)), Times.Once);
    }

    [Fact]
    public async Task Deactivate_LogsAuditEntry()
    {
        var patient = MakePatient(5, tenantId: 3, isActive: true);

        var repositoryMock = new Mock<IPatientRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(5, 3)).ReturnsAsync(patient);

        var auditLoggerMock = new Mock<IAuditLogger>();
        var handler = new DeactivatePatientCommandHandler(repositoryMock.Object, auditLoggerMock.Object);

        await handler.Handle(new DeactivatePatientCommand(5, 3, clinicianId: 9), CancellationToken.None);

        auditLoggerMock.Verify(a => a.LogAsync(
            9, 3, "PatientDeactivated", "Patient", 5,
            It.Is<string?>(details => details != null && details.Contains(patient.MedicalRecordNumber))),
            Times.Once);
    }

    [Fact]
    public async Task Deactivate_PatientBelongsToDifferentTenant_ThrowsPatientNotFoundException()
    {
        var repositoryMock = new Mock<IPatientRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(1, 2)).ReturnsAsync((Patient?)null);

        var handler = new DeactivatePatientCommandHandler(repositoryMock.Object, Mock.Of<IAuditLogger>());

        await Assert.ThrowsAsync<PatientNotFoundException>(
            () => handler.Handle(new DeactivatePatientCommand(1, 2), CancellationToken.None));

        repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Patient>()), Times.Never);
    }

    [Fact]
    public async Task Activate_ValidPatient_SetsIsActiveTrue()
    {
        var patient = MakePatient(1, tenantId: 1, isActive: false);

        var repositoryMock = new Mock<IPatientRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(patient);

        var handler = new ActivatePatientCommandHandler(repositoryMock.Object, Mock.Of<IAuditLogger>());

        var result = await handler.Handle(new ActivatePatientCommand(1, 1), CancellationToken.None);

        Assert.True(result.IsActive);
        repositoryMock.Verify(r => r.UpdateAsync(It.Is<Patient>(p => p.IsActive)), Times.Once);
    }

    [Fact]
    public async Task Activate_LogsAuditEntry()
    {
        var patient = MakePatient(5, tenantId: 3, isActive: false);

        var repositoryMock = new Mock<IPatientRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(5, 3)).ReturnsAsync(patient);

        var auditLoggerMock = new Mock<IAuditLogger>();
        var handler = new ActivatePatientCommandHandler(repositoryMock.Object, auditLoggerMock.Object);

        await handler.Handle(new ActivatePatientCommand(5, 3, clinicianId: 9), CancellationToken.None);

        auditLoggerMock.Verify(a => a.LogAsync(
            9, 3, "PatientActivated", "Patient", 5,
            It.Is<string?>(details => details != null && details.Contains(patient.MedicalRecordNumber))),
            Times.Once);
    }

    [Fact]
    public async Task Activate_PatientBelongsToDifferentTenant_ThrowsPatientNotFoundException()
    {
        var repositoryMock = new Mock<IPatientRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(1, 2)).ReturnsAsync((Patient?)null);

        var handler = new ActivatePatientCommandHandler(repositoryMock.Object, Mock.Of<IAuditLogger>());

        await Assert.ThrowsAsync<PatientNotFoundException>(
            () => handler.Handle(new ActivatePatientCommand(1, 2), CancellationToken.None));

        repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Patient>()), Times.Never);
    }
}
