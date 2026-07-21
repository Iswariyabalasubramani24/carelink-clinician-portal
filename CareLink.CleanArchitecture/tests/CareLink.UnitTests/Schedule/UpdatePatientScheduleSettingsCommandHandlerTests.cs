using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using CareLink.Application.Schedule.Commands;
using CareLink.Domain.Entities;
using Moq;

namespace CareLink.UnitTests.Schedule;

public class UpdatePatientScheduleSettingsCommandHandlerTests
{
    private static Patient MakePatient(int id, int tenantId) => new()
    {
        Id = id,
        TenantId = tenantId,
        MedicalRecordNumber = $"MRN-{id}",
        FirstName = "Test",
        LastName = "Patient",
        DateOfBirth = new DateTime(1970, 1, 1),
        DeviceType = DeviceType.ICD,
        DeviceSerialNumber = $"SN-{id}",
        ImplantDate = new DateTime(2020, 1, 1)
    };

    [Fact]
    public async Task Handle_UseOverrideTrue_UpsertsPatientOverrideAndReturnsIt()
    {
        var patientRepo = new Mock<IPatientRepository>();
        patientRepo.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(MakePatient(1, 1));

        var scheduleRepo = new Mock<IScheduleSettingsRepository>();
        var handler = new UpdatePatientScheduleSettingsCommandHandler(patientRepo.Object, scheduleRepo.Object);

        var result = await handler.Handle(
            new UpdatePatientScheduleSettingsCommand { PatientId = 1, TenantId = 1, UseOverride = true, IntervalDays = 7 },
            CancellationToken.None);

        Assert.Equal(7, result.IntervalDays);
        Assert.True(result.IsOverride);
        scheduleRepo.Verify(r => r.UpsertPatientOverrideAsync(1, 7), Times.Once);
        scheduleRepo.Verify(r => r.RemovePatientOverrideAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UseOverrideFalse_RemovesOverrideAndReturnsClinicDefault()
    {
        var patientRepo = new Mock<IPatientRepository>();
        patientRepo.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(MakePatient(1, 1));

        var scheduleRepo = new Mock<IScheduleSettingsRepository>();
        scheduleRepo.Setup(r => r.GetByTenantIdAsync(1))
            .ReturnsAsync(new PatientScheduleSettings { TenantId = 1, IntervalDays = 60 });

        var handler = new UpdatePatientScheduleSettingsCommandHandler(patientRepo.Object, scheduleRepo.Object);

        var result = await handler.Handle(
            new UpdatePatientScheduleSettingsCommand { PatientId = 1, TenantId = 1, UseOverride = false, IntervalDays = 999 },
            CancellationToken.None);

        Assert.Equal(60, result.IntervalDays);
        Assert.False(result.IsOverride);
        scheduleRepo.Verify(r => r.RemovePatientOverrideAsync(1), Times.Once);
        scheduleRepo.Verify(r => r.UpsertPatientOverrideAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UseOverrideFalseAndNoClinicSettingsRow_FallsBackToHardcodedDefault()
    {
        var patientRepo = new Mock<IPatientRepository>();
        patientRepo.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(MakePatient(1, 1));

        var scheduleRepo = new Mock<IScheduleSettingsRepository>();
        scheduleRepo.Setup(r => r.GetByTenantIdAsync(1)).ReturnsAsync((PatientScheduleSettings?)null);

        var handler = new UpdatePatientScheduleSettingsCommandHandler(patientRepo.Object, scheduleRepo.Object);

        var result = await handler.Handle(
            new UpdatePatientScheduleSettingsCommand { PatientId = 1, TenantId = 1, UseOverride = false, IntervalDays = 999 },
            CancellationToken.None);

        Assert.Equal(30, result.IntervalDays);
        Assert.False(result.IsOverride);
    }

    [Fact]
    public async Task Handle_PatientBelongsToDifferentTenant_ThrowsPatientNotFoundExceptionAndMakesNoChanges()
    {
        var patientRepo = new Mock<IPatientRepository>();
        patientRepo.Setup(r => r.GetByIdAsync(1, 2)).ReturnsAsync((Patient?)null);

        var scheduleRepo = new Mock<IScheduleSettingsRepository>();
        var handler = new UpdatePatientScheduleSettingsCommandHandler(patientRepo.Object, scheduleRepo.Object);

        await Assert.ThrowsAsync<PatientNotFoundException>(() => handler.Handle(
            new UpdatePatientScheduleSettingsCommand { PatientId = 1, TenantId = 2, UseOverride = true, IntervalDays = 7 },
            CancellationToken.None));

        scheduleRepo.Verify(r => r.UpsertPatientOverrideAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        scheduleRepo.Verify(r => r.RemovePatientOverrideAsync(It.IsAny<int>()), Times.Never);
    }
}
