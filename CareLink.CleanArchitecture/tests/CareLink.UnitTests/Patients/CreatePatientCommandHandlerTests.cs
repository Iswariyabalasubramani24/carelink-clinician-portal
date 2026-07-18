using CareLink.Application.Common.Interfaces;
using CareLink.Application.Patients.Commands;
using CareLink.Domain.Entities;
using Moq;

namespace CareLink.UnitTests.Patients;

public class CreatePatientCommandHandlerTests
{
    private static CreatePatientCommand ValidCommand(int tenantId = 1) => new()
    {
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
        LastHeartRate = 72
    };

    [Fact]
    public async Task Handle_ValidCommand_CreatesPatientAndReturnsCorrectDto()
    {
        var repositoryMock = new Mock<IPatientRepository>();
        repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Patient>()))
            .Returns((Patient patient) =>
            {
                patient.Id = 42;
                return Task.FromResult(patient);
            });

        var handler = new CreatePatientCommandHandler(repositoryMock.Object);
        var command = ValidCommand();

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(42, result.Id);
        Assert.Equal(command.MedicalRecordNumber, result.MedicalRecordNumber);
        Assert.Equal(command.FirstName, result.FirstName);
        Assert.Equal(command.LastName, result.LastName);
        Assert.Equal(command.DateOfBirth, result.DateOfBirth);
        Assert.Equal(command.PhoneNumber, result.PhoneNumber);
        Assert.Equal(command.Email, result.Email);
        Assert.Equal(command.DeviceType, result.DeviceType);
        Assert.Equal(command.DeviceManufacturer, result.DeviceManufacturer);
        Assert.Equal(command.DeviceModel, result.DeviceModel);
        Assert.Equal(command.DeviceSerialNumber, result.DeviceSerialNumber);
        Assert.Equal(command.ImplantDate, result.ImplantDate);
        Assert.Equal(command.BatteryLevel, result.BatteryLevel);
        Assert.Equal(command.LastHeartRate, result.LastHeartRate);
        Assert.True(result.IsActive);

        repositoryMock.Verify(r => r.AddAsync(It.IsAny<Patient>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_AssociatesPatientWithGivenTenantId()
    {
        var repositoryMock = new Mock<IPatientRepository>();
        repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Patient>()))
            .Returns((Patient patient) =>
            {
                patient.Id = 7;
                return Task.FromResult(patient);
            });

        var handler = new CreatePatientCommandHandler(repositoryMock.Object);
        var command = ValidCommand(tenantId: 99);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(99, result.TenantId);
        repositoryMock.Verify(r => r.AddAsync(It.Is<Patient>(p => p.TenantId == 99)), Times.Once);
    }
}
