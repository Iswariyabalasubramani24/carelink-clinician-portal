using CareLink.Application.Common.Interfaces;
using CareLink.Application.Dashboard.Queries;
using CareLink.Domain.Entities;
using Moq;

namespace CareLink.UnitTests.Dashboard;

public class GetDashboardSummaryQueryHandlerTests
{
    private static Patient MakePatient(
        int id,
        DateTime createdAt,
        DateTime? lastSyncedAt,
        bool isActive = true) => new()
    {
        Id = id,
        TenantId = 1,
        MedicalRecordNumber = $"MRN-{id}",
        FirstName = "Test",
        LastName = $"Patient{id}",
        DateOfBirth = new DateTime(1970, 1, 1),
        DeviceType = DeviceType.Pacemaker,
        DeviceSerialNumber = $"SN-{id}",
        ImplantDate = new DateTime(2020, 1, 1),
        CreatedAt = createdAt,
        LastSyncedAt = lastSyncedAt,
        IsActive = isActive
    };

    [Fact]
    public async Task Handle_MixOfPatients_ComputesAllThreeCountsCorrectly()
    {
        var now = DateTime.UtcNow;

        var patients = new List<Patient>
        {
            // New (created 2 days ago) and connected (synced 1 day ago).
            MakePatient(1, createdAt: now.AddDays(-2), lastSyncedAt: now.AddDays(-1)),

            // Not new, and disconnected because it has never synced.
            MakePatient(2, createdAt: now.AddDays(-30), lastSyncedAt: null),

            // Not new, and disconnected because its last sync is over 7 days old.
            MakePatient(3, createdAt: now.AddDays(-60), lastSyncedAt: now.AddDays(-10)),

            // Not new, and connected (synced within the last 7 days).
            MakePatient(4, createdAt: now.AddDays(-60), lastSyncedAt: now.AddHours(-1)),

            // Would be "new" and "disconnected" by date alone, but inactive so it
            // must be excluded from every count.
            MakePatient(5, createdAt: now.AddDays(-1), lastSyncedAt: null, isActive: false)
        };

        var repositoryMock = new Mock<IPatientRepository>();
        repositoryMock.Setup(r => r.GetByTenantIdAsync(1)).ReturnsAsync(patients);

        var handler = new GetDashboardSummaryQueryHandler(repositoryMock.Object);

        var result = await handler.Handle(new GetDashboardSummaryQuery(1), CancellationToken.None);

        Assert.Equal(1, result.NewPatientsCount);
        Assert.Equal(2, result.DisconnectedMonitorsCount);
        Assert.Equal(4, result.TotalActivePatientsCount);
    }

    [Fact]
    public async Task Handle_NoPatients_ReturnsAllZero()
    {
        var repositoryMock = new Mock<IPatientRepository>();
        repositoryMock.Setup(r => r.GetByTenantIdAsync(1)).ReturnsAsync(new List<Patient>());

        var handler = new GetDashboardSummaryQueryHandler(repositoryMock.Object);

        var result = await handler.Handle(new GetDashboardSummaryQuery(1), CancellationToken.None);

        Assert.Equal(0, result.NewPatientsCount);
        Assert.Equal(0, result.DisconnectedMonitorsCount);
        Assert.Equal(0, result.TotalActivePatientsCount);
    }
}
