using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using CareLink.Application.Patients.Commands;
using CareLink.Domain.Entities;
using Moq;

namespace CareLink.UnitTests.Patients;

public class UpdatePatientCommandHandlerTests
{
    private static Patient MakeExistingPatient(int id, int tenantId) => new()
    {
        Id = id,
        TenantId = tenantId,
        MedicalRecordNumber = "APL-1001",
        FirstName = "Rajesh",
        LastName = "Kumar",
        DateOfBirth = new DateTime(1965, 4, 12),
        PhoneNumber = "+91-9876543210",
        Email = "rajesh.kumar@example.com",
        DeviceType = DeviceType.ICD,
        DeviceManufacturer = "Medtronic",
        DeviceModel = "Evera XT",
        DeviceSerialNumber = "MDT-ICD-0001",
        ImplantDate = new DateTime(2022, 3, 15),
        BatteryLevel = 87.5m,
        LastHeartRate = 72,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    private static UpdatePatientCommand ValidCommand(int patientId, int tenantId, int clinicianId = 9) => new()
    {
        PatientId = patientId,
        TenantId = tenantId,
        ClinicianId = clinicianId,
        MedicalRecordNumber = "APL-1001-B",
        FirstName = "Rajesh",
        LastName = "Kumar-Singh",
        DateOfBirth = new DateTime(1965, 4, 12),
        PhoneNumber = "+91-9999999999",
        Email = "rajesh.new@example.com",
        DeviceType = DeviceType.Pacemaker,
        DeviceManufacturer = "Boston Scientific",
        DeviceModel = "Accolade MRI",
        DeviceSerialNumber = "BSX-PM-0002",
        ImplantDate = new DateTime(2023, 1, 1)
    };

    [Fact]
    public async Task Handle_ValidCommand_UpdatesEditableFieldsAndPreservesTelemetry()
    {
        var patient = MakeExistingPatient(1, tenantId: 1);

        var repositoryMock = new Mock<IPatientRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(patient);

        var handler = new UpdatePatientCommandHandler(repositoryMock.Object, Mock.Of<IAuditLogger>());
        var command = ValidCommand(1, 1);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(command.MedicalRecordNumber, result.MedicalRecordNumber);
        Assert.Equal(command.FirstName, result.FirstName);
        Assert.Equal(command.LastName, result.LastName);
        Assert.Equal(command.PhoneNumber, result.PhoneNumber);
        Assert.Equal(command.Email, result.Email);
        Assert.Equal(command.DeviceType, result.DeviceType);
        Assert.Equal(command.DeviceManufacturer, result.DeviceManufacturer);
        Assert.Equal(command.DeviceModel, result.DeviceModel);
        Assert.Equal(command.DeviceSerialNumber, result.DeviceSerialNumber);
        Assert.Equal(command.ImplantDate, result.ImplantDate);

        // Device telemetry is not part of the edit form and must survive untouched.
        Assert.Equal(87.5m, result.BatteryLevel);
        Assert.Equal(72, result.LastHeartRate);

        Assert.NotNull(result.UpdatedAt);
        repositoryMock.Verify(r => r.UpdateAsync(patient), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_LogsAuditEntry()
    {
        var patient = MakeExistingPatient(5, tenantId: 3);

        var repositoryMock = new Mock<IPatientRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(5, 3)).ReturnsAsync(patient);

        var auditLoggerMock = new Mock<IAuditLogger>();
        var handler = new UpdatePatientCommandHandler(repositoryMock.Object, auditLoggerMock.Object);
        var command = ValidCommand(5, 3, clinicianId: 11);

        await handler.Handle(command, CancellationToken.None);

        auditLoggerMock.Verify(a => a.LogAsync(
            11, 3, "PatientUpdated", "Patient", 5,
            It.Is<string?>(details => details != null && details.Contains(command.MedicalRecordNumber))),
            Times.Once);
    }

    [Fact]
    public async Task Handle_PatientBelongsToDifferentTenant_ThrowsPatientNotFoundException()
    {
        // The lookup is tenant-scoped, so a clinician can never edit a patient
        // belonging to a different hospital.
        var repositoryMock = new Mock<IPatientRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(1, 2)).ReturnsAsync((Patient?)null);

        var handler = new UpdatePatientCommandHandler(repositoryMock.Object, Mock.Of<IAuditLogger>());

        await Assert.ThrowsAsync<PatientNotFoundException>(
            () => handler.Handle(ValidCommand(1, 2), CancellationToken.None));

        repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Patient>()), Times.Never);
    }
}
