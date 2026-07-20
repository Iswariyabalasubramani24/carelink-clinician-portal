using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using CareLink.Application.Patients.Queries;
using CareLink.Domain.Entities;
using Moq;

namespace CareLink.UnitTests.Patients;

public class GetPatientTransmissionHistoryQueryHandlerTests
{
    private static Patient MakePatient(int id, int tenantId = 1) => new()
    {
        Id = id,
        TenantId = tenantId,
        MedicalRecordNumber = $"MRN-{id}",
        FirstName = "Test",
        LastName = "Patient",
        DateOfBirth = new DateTime(1970, 1, 1),
        DeviceType = DeviceType.ICD,
        DeviceSerialNumber = $"SN-{id}",
        ImplantDate = new DateTime(2020, 1, 1),
        CreatedAt = DateTime.UtcNow.AddYears(-1),
        LastHeartRate = 72,
        BatteryLevel = 88.5m
    };

    [Fact]
    public async Task Handle_ExistingPatient_ReturnsBetween10And15PointsWithinPast90Days()
    {
        var patient = MakePatient(1);

        var repositoryMock = new Mock<IPatientRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(patient);

        var handler = new GetPatientTransmissionHistoryQueryHandler(repositoryMock.Object);

        var result = await handler.Handle(new GetPatientTransmissionHistoryQuery(1, 1), CancellationToken.None);

        Assert.InRange(result.Count, 10, 15);

        var today = DateTime.UtcNow.Date;
        var earliestAllowed = today.AddDays(-90);
        Assert.All(result, point => Assert.InRange(point.Date, earliestAllowed, today));

        // Points should be in chronological order (oldest first).
        for (var i = 1; i < result.Count; i++)
        {
            Assert.True(result[i].Date >= result[i - 1].Date);
        }
    }

    [Fact]
    public async Task Handle_MostRecentPoint_MatchesPatientsCurrentReadings()
    {
        var patient = MakePatient(1);

        var repositoryMock = new Mock<IPatientRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(patient);

        var handler = new GetPatientTransmissionHistoryQueryHandler(repositoryMock.Object);

        var result = await handler.Handle(new GetPatientTransmissionHistoryQuery(1, 1), CancellationToken.None);

        var mostRecent = result[^1];
        Assert.Equal(patient.LastHeartRate, mostRecent.HeartRate);
        Assert.Equal(patient.BatteryLevel, mostRecent.BatteryLevel);
    }

    [Fact]
    public async Task Handle_SamePatientCalledTwice_ProducesIdenticalSeriesEachTime()
    {
        var patient = MakePatient(42);

        var repositoryMock = new Mock<IPatientRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(42, 1)).ReturnsAsync(patient);

        var handler = new GetPatientTransmissionHistoryQueryHandler(repositoryMock.Object);

        var first = await handler.Handle(new GetPatientTransmissionHistoryQuery(42, 1), CancellationToken.None);
        var second = await handler.Handle(new GetPatientTransmissionHistoryQuery(42, 1), CancellationToken.None);

        Assert.Equal(first, second);
    }

    [Fact]
    public async Task Handle_DifferentPatientIds_ProduceDifferentSeries()
    {
        var patientA = MakePatient(1);
        var patientB = MakePatient(2);

        var repositoryMock = new Mock<IPatientRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(patientA);
        repositoryMock.Setup(r => r.GetByIdAsync(2, 1)).ReturnsAsync(patientB);

        var handler = new GetPatientTransmissionHistoryQueryHandler(repositoryMock.Object);

        var seriesA = await handler.Handle(new GetPatientTransmissionHistoryQuery(1, 1), CancellationToken.None);
        var seriesB = await handler.Handle(new GetPatientTransmissionHistoryQuery(2, 1), CancellationToken.None);

        Assert.NotEqual(seriesA, seriesB);
    }

    [Fact]
    public async Task Handle_PatientNotFoundForTenant_ThrowsPatientNotFoundException()
    {
        var repositoryMock = new Mock<IPatientRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync((Patient?)null);

        var handler = new GetPatientTransmissionHistoryQueryHandler(repositoryMock.Object);

        await Assert.ThrowsAsync<PatientNotFoundException>(
            () => handler.Handle(new GetPatientTransmissionHistoryQuery(1, 1), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_PatientBelongsToDifferentTenant_ThrowsPatientNotFoundException()
    {
        // The repository is tenant-scoped, so a patient that exists but belongs to
        // a different tenant must come back as "not found" here, never leaked.
        var repositoryMock = new Mock<IPatientRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(1, 2)).ReturnsAsync((Patient?)null);

        var handler = new GetPatientTransmissionHistoryQueryHandler(repositoryMock.Object);

        await Assert.ThrowsAsync<PatientNotFoundException>(
            () => handler.Handle(new GetPatientTransmissionHistoryQuery(1, 2), CancellationToken.None));

        repositoryMock.Verify(r => r.GetByIdAsync(1, 2), Times.Once);
    }
}
