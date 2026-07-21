using CareLink.Application.Common.Interfaces;
using CareLink.Application.Schedule.Queries;
using CareLink.Domain.Entities;
using Moq;

namespace CareLink.UnitTests.Schedule;

public class GetTransmissionScheduleQueryHandlerTests
{
    private static Patient MakePatient(int id, int tenantId, string firstName, string lastName, DateTime? lastSyncedAt) => new()
    {
        Id = id,
        TenantId = tenantId,
        MedicalRecordNumber = $"MRN-{id}",
        FirstName = firstName,
        LastName = lastName,
        DateOfBirth = new DateTime(1970, 1, 1),
        DeviceType = DeviceType.ICD,
        DeviceSerialNumber = $"SN-{id}",
        ImplantDate = new DateTime(2020, 1, 1),
        LastSyncedAt = lastSyncedAt
    };

    [Theory]
    [InlineData("2026-01-01", 30, "2026-01-31")]
    [InlineData("2026-01-01", 1, "2026-01-02")]
    [InlineData("2026-01-15", 90, "2026-04-15")]
    public void CalculateNextScheduledDate_GivenLastSyncedAtAndInterval_AddsIntervalDaysToLastSync(
        string lastSyncedAt, int intervalDays, string expected)
    {
        var result = GetTransmissionScheduleQueryHandler.CalculateNextScheduledDate(DateTime.Parse(lastSyncedAt), intervalDays);

        Assert.Equal(DateTime.Parse(expected), result);
    }

    [Fact]
    public void CalculateNextScheduledDate_NoLastSyncedAt_ReturnsNull()
    {
        var result = GetTransmissionScheduleQueryHandler.CalculateNextScheduledDate(null, 30);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_PatientWithOverride_UsesOverrideIntervalNotClinicDefault()
    {
        var lastSync = new DateTime(2026, 1, 1);
        var patients = new List<Patient> { MakePatient(1, 1, "Rajesh", "Kumar", lastSync) };

        var patientRepo = new Mock<IPatientRepository>();
        patientRepo.Setup(r => r.GetByTenantIdAsync(1)).ReturnsAsync(patients);

        var scheduleRepo = new Mock<IScheduleSettingsRepository>();
        scheduleRepo.Setup(r => r.GetByPatientIdsAsync(It.Is<IEnumerable<int>>(ids => ids.Contains(1))))
            .ReturnsAsync([new PatientScheduleSettings { PatientId = 1, IntervalDays = 10 }]);
        scheduleRepo.Setup(r => r.GetByTenantIdAsync(1))
            .ReturnsAsync(new PatientScheduleSettings { TenantId = 1, IntervalDays = 30 });

        var handler = new GetTransmissionScheduleQueryHandler(patientRepo.Object, scheduleRepo.Object);

        var result = await handler.Handle(new GetTransmissionScheduleQuery(1), CancellationToken.None);

        var entry = Assert.Single(result);
        Assert.Equal(10, entry.IntervalDays);
        Assert.Equal(lastSync.AddDays(10), entry.NextScheduledDate);
    }

    [Fact]
    public async Task Handle_PatientWithoutOverride_FallsBackToClinicDefault()
    {
        var lastSync = new DateTime(2026, 1, 1);
        var patients = new List<Patient> { MakePatient(1, 1, "Rajesh", "Kumar", lastSync) };

        var patientRepo = new Mock<IPatientRepository>();
        patientRepo.Setup(r => r.GetByTenantIdAsync(1)).ReturnsAsync(patients);

        var scheduleRepo = new Mock<IScheduleSettingsRepository>();
        scheduleRepo.Setup(r => r.GetByPatientIdsAsync(It.IsAny<IEnumerable<int>>())).ReturnsAsync([]);
        scheduleRepo.Setup(r => r.GetByTenantIdAsync(1))
            .ReturnsAsync(new PatientScheduleSettings { TenantId = 1, IntervalDays = 45 });

        var handler = new GetTransmissionScheduleQueryHandler(patientRepo.Object, scheduleRepo.Object);

        var result = await handler.Handle(new GetTransmissionScheduleQuery(1), CancellationToken.None);

        var entry = Assert.Single(result);
        Assert.Equal(45, entry.IntervalDays);
        Assert.Equal(lastSync.AddDays(45), entry.NextScheduledDate);
    }

    [Fact]
    public async Task Handle_NoOverrideAndNoClinicSettingsRow_FallsBackToHardcodedDefault()
    {
        var lastSync = new DateTime(2026, 1, 1);
        var patients = new List<Patient> { MakePatient(1, 1, "Rajesh", "Kumar", lastSync) };

        var patientRepo = new Mock<IPatientRepository>();
        patientRepo.Setup(r => r.GetByTenantIdAsync(1)).ReturnsAsync(patients);

        var scheduleRepo = new Mock<IScheduleSettingsRepository>();
        scheduleRepo.Setup(r => r.GetByPatientIdsAsync(It.IsAny<IEnumerable<int>>())).ReturnsAsync([]);
        scheduleRepo.Setup(r => r.GetByTenantIdAsync(1)).ReturnsAsync((PatientScheduleSettings?)null);

        var handler = new GetTransmissionScheduleQueryHandler(patientRepo.Object, scheduleRepo.Object);

        var result = await handler.Handle(new GetTransmissionScheduleQuery(1), CancellationToken.None);

        var entry = Assert.Single(result);
        Assert.Equal(30, entry.IntervalDays);
        Assert.Equal(lastSync.AddDays(30), entry.NextScheduledDate);
    }

    [Fact]
    public async Task Handle_PatientNeverSynced_ReturnsNullNextScheduledDate()
    {
        var patients = new List<Patient> { MakePatient(1, 1, "Rajesh", "Kumar", lastSyncedAt: null) };

        var patientRepo = new Mock<IPatientRepository>();
        patientRepo.Setup(r => r.GetByTenantIdAsync(1)).ReturnsAsync(patients);

        var scheduleRepo = new Mock<IScheduleSettingsRepository>();
        scheduleRepo.Setup(r => r.GetByPatientIdsAsync(It.IsAny<IEnumerable<int>>())).ReturnsAsync([]);
        scheduleRepo.Setup(r => r.GetByTenantIdAsync(1)).ReturnsAsync((PatientScheduleSettings?)null);

        var handler = new GetTransmissionScheduleQueryHandler(patientRepo.Object, scheduleRepo.Object);

        var result = await handler.Handle(new GetTransmissionScheduleQuery(1), CancellationToken.None);

        var entry = Assert.Single(result);
        Assert.Null(entry.NextScheduledDate);
    }

    [Fact]
    public async Task Handle_MultiplePatients_SortsBySoonestNextScheduledDateWithNeverSyncedLast()
    {
        var patients = new List<Patient>
        {
            MakePatient(1, 1, "Due", "Later", new DateTime(2026, 1, 1)),      // +30d -> 2026-01-31
            MakePatient(2, 1, "Due", "Soonest", new DateTime(2025, 12, 20)),  // +30d -> 2026-01-19
            MakePatient(3, 1, "Never", "Synced", null)
        };

        var patientRepo = new Mock<IPatientRepository>();
        patientRepo.Setup(r => r.GetByTenantIdAsync(1)).ReturnsAsync(patients);

        var scheduleRepo = new Mock<IScheduleSettingsRepository>();
        scheduleRepo.Setup(r => r.GetByPatientIdsAsync(It.IsAny<IEnumerable<int>>())).ReturnsAsync([]);
        scheduleRepo.Setup(r => r.GetByTenantIdAsync(1))
            .ReturnsAsync(new PatientScheduleSettings { TenantId = 1, IntervalDays = 30 });

        var handler = new GetTransmissionScheduleQueryHandler(patientRepo.Object, scheduleRepo.Object);

        var result = await handler.Handle(new GetTransmissionScheduleQuery(1), CancellationToken.None);

        Assert.Equal([2, 1, 3], result.Select(e => e.PatientId));
    }

    [Fact]
    public async Task Handle_TenantWithNoPatients_ReturnsEmptyList()
    {
        var patientRepo = new Mock<IPatientRepository>();
        patientRepo.Setup(r => r.GetByTenantIdAsync(1)).ReturnsAsync([]);

        var scheduleRepo = new Mock<IScheduleSettingsRepository>();
        scheduleRepo.Setup(r => r.GetByPatientIdsAsync(It.IsAny<IEnumerable<int>>())).ReturnsAsync([]);
        scheduleRepo.Setup(r => r.GetByTenantIdAsync(1)).ReturnsAsync((PatientScheduleSettings?)null);

        var handler = new GetTransmissionScheduleQueryHandler(patientRepo.Object, scheduleRepo.Object);

        var result = await handler.Handle(new GetTransmissionScheduleQuery(1), CancellationToken.None);

        Assert.Empty(result);
        scheduleRepo.Verify(r => r.GetByPatientIdsAsync(It.IsAny<IEnumerable<int>>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DifferentTenants_OnlyQueryTheirOwnTenantsPatientsAndSettings()
    {
        var patientRepo = new Mock<IPatientRepository>();
        patientRepo.Setup(r => r.GetByTenantIdAsync(1))
            .ReturnsAsync([MakePatient(1, 1, "Apollo", "Patient", new DateTime(2026, 1, 1))]);
        patientRepo.Setup(r => r.GetByTenantIdAsync(2))
            .ReturnsAsync([MakePatient(2, 2, "Charite", "Patient", new DateTime(2026, 1, 1))]);

        var scheduleRepo = new Mock<IScheduleSettingsRepository>();
        scheduleRepo.Setup(r => r.GetByPatientIdsAsync(It.IsAny<IEnumerable<int>>())).ReturnsAsync([]);
        scheduleRepo.Setup(r => r.GetByTenantIdAsync(It.IsAny<int>())).ReturnsAsync((PatientScheduleSettings?)null);

        var handler = new GetTransmissionScheduleQueryHandler(patientRepo.Object, scheduleRepo.Object);

        var tenant1Result = await handler.Handle(new GetTransmissionScheduleQuery(1), CancellationToken.None);
        var tenant2Result = await handler.Handle(new GetTransmissionScheduleQuery(2), CancellationToken.None);

        Assert.Single(tenant1Result);
        Assert.Equal(1, tenant1Result[0].PatientId);
        Assert.Single(tenant2Result);
        Assert.Equal(2, tenant2Result[0].PatientId);
        patientRepo.Verify(r => r.GetByTenantIdAsync(1), Times.Once);
        patientRepo.Verify(r => r.GetByTenantIdAsync(2), Times.Once);
    }
}
