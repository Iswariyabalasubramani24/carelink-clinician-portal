using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using CareLink.Application.Schedule.Queries;
using CareLink.Domain.Entities;
using Moq;

namespace CareLink.UnitTests.Schedule;

public class GetPatientScheduleSettingsQueryHandlerTests
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
    public async Task Handle_PatientHasOverride_ReturnsOverrideIntervalWithIsOverrideTrue()
    {
        var patientRepo = new Mock<IPatientRepository>();
        patientRepo.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(MakePatient(1, 1));

        var scheduleRepo = new Mock<IScheduleSettingsRepository>();
        scheduleRepo.Setup(r => r.GetByPatientIdAsync(1))
            .ReturnsAsync(new PatientScheduleSettings { PatientId = 1, IntervalDays = 14 });

        var handler = new GetPatientScheduleSettingsQueryHandler(patientRepo.Object, scheduleRepo.Object);

        var result = await handler.Handle(new GetPatientScheduleSettingsQuery(1, 1), CancellationToken.None);

        Assert.Equal(14, result.IntervalDays);
        Assert.True(result.IsOverride);
        // An override exists, so the clinic-wide default must never be consulted.
        scheduleRepo.Verify(r => r.GetByTenantIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NoOverride_FallsBackToClinicDefault()
    {
        var patientRepo = new Mock<IPatientRepository>();
        patientRepo.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(MakePatient(1, 1));

        var scheduleRepo = new Mock<IScheduleSettingsRepository>();
        scheduleRepo.Setup(r => r.GetByPatientIdAsync(1)).ReturnsAsync((PatientScheduleSettings?)null);
        scheduleRepo.Setup(r => r.GetByTenantIdAsync(1))
            .ReturnsAsync(new PatientScheduleSettings { TenantId = 1, IntervalDays = 45 });

        var handler = new GetPatientScheduleSettingsQueryHandler(patientRepo.Object, scheduleRepo.Object);

        var result = await handler.Handle(new GetPatientScheduleSettingsQuery(1, 1), CancellationToken.None);

        Assert.Equal(45, result.IntervalDays);
        Assert.False(result.IsOverride);
    }

    [Fact]
    public async Task Handle_NoOverrideAndNoClinicSettingsRow_FallsBackToHardcodedDefault()
    {
        var patientRepo = new Mock<IPatientRepository>();
        patientRepo.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(MakePatient(1, 1));

        var scheduleRepo = new Mock<IScheduleSettingsRepository>();
        scheduleRepo.Setup(r => r.GetByPatientIdAsync(1)).ReturnsAsync((PatientScheduleSettings?)null);
        scheduleRepo.Setup(r => r.GetByTenantIdAsync(1)).ReturnsAsync((PatientScheduleSettings?)null);

        var handler = new GetPatientScheduleSettingsQueryHandler(patientRepo.Object, scheduleRepo.Object);

        var result = await handler.Handle(new GetPatientScheduleSettingsQuery(1, 1), CancellationToken.None);

        Assert.Equal(30, result.IntervalDays);
        Assert.False(result.IsOverride);
    }

    [Fact]
    public async Task Handle_PatientNotFoundForTenant_ThrowsPatientNotFoundException()
    {
        var patientRepo = new Mock<IPatientRepository>();
        patientRepo.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync((Patient?)null);

        var scheduleRepo = new Mock<IScheduleSettingsRepository>();
        var handler = new GetPatientScheduleSettingsQueryHandler(patientRepo.Object, scheduleRepo.Object);

        await Assert.ThrowsAsync<PatientNotFoundException>(
            () => handler.Handle(new GetPatientScheduleSettingsQuery(1, 1), CancellationToken.None));

        scheduleRepo.Verify(r => r.GetByPatientIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PatientBelongsToDifferentTenant_ThrowsPatientNotFoundException()
    {
        // The repository is tenant-scoped, so a patient that exists but belongs to a
        // different tenant must come back as "not found" here, never leaked or acted on.
        var patientRepo = new Mock<IPatientRepository>();
        patientRepo.Setup(r => r.GetByIdAsync(1, 2)).ReturnsAsync((Patient?)null);

        var scheduleRepo = new Mock<IScheduleSettingsRepository>();
        var handler = new GetPatientScheduleSettingsQueryHandler(patientRepo.Object, scheduleRepo.Object);

        await Assert.ThrowsAsync<PatientNotFoundException>(
            () => handler.Handle(new GetPatientScheduleSettingsQuery(1, 2), CancellationToken.None));
    }
}
