using CareLink.Application.Alerts.Queries;
using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using Moq;

namespace CareLink.UnitTests.Alerts;

public class GetPatientAlertsQueryHandlerTests
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
        ImplantDate = new DateTime(2020, 1, 1),
        CreatedAt = DateTime.UtcNow.AddYears(-1)
    };

    [Fact]
    public async Task Handle_PatientInTenant_ReturnsAlertsFromEvaluationService()
    {
        var patient = MakePatient(1, 1);
        var alerts = new List<Alert>
        {
            new() { Id = 1, PatientId = 1, TenantId = 1, AlertType = AlertType.LowBattery, Urgency = AlertUrgency.Red }
        };

        var patientRepoMock = new Mock<IPatientRepository>();
        patientRepoMock.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(patient);

        var alertEvaluationServiceMock = new Mock<IAlertEvaluationService>();
        alertEvaluationServiceMock.Setup(s => s.GetActiveAlertsForPatientAsync(patient)).ReturnsAsync(alerts);

        var handler = new GetPatientAlertsQueryHandler(patientRepoMock.Object, alertEvaluationServiceMock.Object);

        var result = await handler.Handle(new GetPatientAlertsQuery(1, 1), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(AlertType.LowBattery, result[0].AlertType);
    }

    [Fact]
    public async Task Handle_PatientBelongsToDifferentTenant_ThrowsPatientNotFoundException()
    {
        // IPatientRepository.GetByIdAsync is tenant-scoped, so a patient that exists
        // but belongs to a different tenant comes back null here - it must never
        // be possible to read another hospital's patient alerts by guessing an id.
        var patientRepoMock = new Mock<IPatientRepository>();
        patientRepoMock.Setup(r => r.GetByIdAsync(1, 2)).ReturnsAsync((Patient?)null);

        var alertEvaluationServiceMock = new Mock<IAlertEvaluationService>();

        var handler = new GetPatientAlertsQueryHandler(patientRepoMock.Object, alertEvaluationServiceMock.Object);

        await Assert.ThrowsAsync<PatientNotFoundException>(
            () => handler.Handle(new GetPatientAlertsQuery(1, 2), CancellationToken.None));

        alertEvaluationServiceMock.Verify(s => s.GetActiveAlertsForPatientAsync(It.IsAny<Patient>()), Times.Never);
    }
}
