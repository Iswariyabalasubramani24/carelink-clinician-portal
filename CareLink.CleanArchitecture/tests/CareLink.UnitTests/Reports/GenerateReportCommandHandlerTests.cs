using System.Text.Json;
using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using CareLink.Application.Patients;
using CareLink.Application.Patients.Queries;
using CareLink.Application.Reports;
using CareLink.Application.Reports.Commands;
using CareLink.Domain.Entities;
using MediatR;
using Moq;

namespace CareLink.UnitTests.Reports;

public class GenerateReportCommandHandlerTests
{
    private static Patient MakePatient(int id = 1, int tenantId = 1) => new()
    {
        Id = id,
        TenantId = tenantId,
        MedicalRecordNumber = $"MRN-{id}",
        FirstName = "Test",
        LastName = "Patient",
        DateOfBirth = new DateTime(1970, 1, 1),
        DeviceType = DeviceType.ICD,
        DeviceManufacturer = "Medtronic",
        DeviceModel = "Evera",
        DeviceSerialNumber = $"SN-{id}",
        ImplantDate = new DateTime(2020, 1, 1),
        CreatedAt = DateTime.UtcNow.AddYears(-1),
        LastHeartRate = 72,
        BatteryLevel = 88.5m,
        LastSyncedAt = DateTime.UtcNow.AddDays(-1)
    };

    private static Alert MakeAlert(int patientId, int tenantId) => new()
    {
        Id = 1,
        PatientId = patientId,
        TenantId = tenantId,
        AlertType = AlertType.LowBattery,
        Urgency = AlertUrgency.Yellow,
        TriggeredAt = DateTime.UtcNow.AddDays(-2),
        IsAcknowledged = true,
        AcknowledgedAt = DateTime.UtcNow.AddDays(-1)
    };

    private static (
        Mock<IPatientRepository> patientRepo,
        Mock<ITenantRepository> tenantRepo,
        Mock<IAlertRepository> alertRepo,
        Mock<IAlertEvaluationService> alertEvaluationService,
        Mock<IReportRepository> reportRepo,
        Mock<IAuditLogger> auditLogger,
        Mock<IMediator> mediator) MakeMocks()
    {
        return (new Mock<IPatientRepository>(), new Mock<ITenantRepository>(), new Mock<IAlertRepository>(),
            new Mock<IAlertEvaluationService>(), new Mock<IReportRepository>(), new Mock<IAuditLogger>(), new Mock<IMediator>());
    }

    [Theory]
    [InlineData(ReportType.FullReport)]
    [InlineData(ReportType.SummaryReport)]
    [InlineData(ReportType.EventReport)]
    public async Task Handle_ValidRequest_CapturesPatientTenantTransmissionAndAlertDataInSnapshot(ReportType reportType)
    {
        var patient = MakePatient();
        var tenant = new Tenant { Id = 1, Name = "Apollo Hospital", Region = "APAC", LanguageCode = "en" };
        var transmissionHistory = new List<TransmissionHistoryDto>
        {
            new(DateTime.UtcNow.AddDays(-30), 70, 90m),
            new(DateTime.UtcNow, 72, 88.5m)
        };
        var alerts = new List<Alert> { MakeAlert(patient.Id, patient.TenantId) };

        var (patientRepo, tenantRepo, alertRepo, alertEvaluationService, reportRepo, auditLogger, mediator) = MakeMocks();
        patientRepo.Setup(r => r.GetByIdAsync(patient.Id, patient.TenantId)).ReturnsAsync(patient);
        tenantRepo.Setup(r => r.GetByIdAsync(patient.TenantId)).ReturnsAsync(tenant);
        mediator.Setup(m => m.Send(It.IsAny<GetPatientTransmissionHistoryQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(transmissionHistory);
        alertEvaluationService.Setup(s => s.GetActiveAlertsForPatientAsync(patient)).ReturnsAsync(alerts);
        alertRepo.Setup(r => r.GetByPatientIdAsync(patient.Id)).ReturnsAsync(alerts);

        Report? capturedReport = null;
        reportRepo.Setup(r => r.AddAsync(It.IsAny<Report>()))
            .Callback<Report>(r => { r.Id = 42; capturedReport = r; })
            .ReturnsAsync((Report r) => r);

        var handler = new GenerateReportCommandHandler(
            patientRepo.Object, tenantRepo.Object, alertRepo.Object, alertEvaluationService.Object, reportRepo.Object, auditLogger.Object, mediator.Object);

        var result = await handler.Handle(new GenerateReportCommand(patient.Id, patient.TenantId, reportType), CancellationToken.None);

        Assert.Equal(42, result.Id);
        Assert.Equal(reportType, result.ReportType);
        Assert.NotNull(capturedReport);

        var snapshot = JsonSerializer.Deserialize<ReportSnapshotDto>(capturedReport!.DataSnapshot);
        Assert.NotNull(snapshot);
        Assert.Equal(tenant.Name, snapshot!.TenantName);
        Assert.Equal(patient.MedicalRecordNumber, snapshot.MedicalRecordNumber);
        Assert.Equal(transmissionHistory.Count, snapshot.TransmissionHistory.Count);
        Assert.Single(snapshot.Alerts);
        Assert.Equal(nameof(AlertType.LowBattery), snapshot.Alerts[0].AlertType);

        // Alert conditions must be freshly evaluated before the full history is
        // captured, so any alert active right now but never persisted isn't missed.
        alertEvaluationService.Verify(s => s.GetActiveAlertsForPatientAsync(patient), Times.Once);
    }

    [Fact]
    public async Task Handle_PatientNotFoundForTenant_ThrowsPatientNotFoundException()
    {
        var (patientRepo, tenantRepo, alertRepo, alertEvaluationService, reportRepo, auditLogger, mediator) = MakeMocks();
        patientRepo.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync((Patient?)null);

        var handler = new GenerateReportCommandHandler(
            patientRepo.Object, tenantRepo.Object, alertRepo.Object, alertEvaluationService.Object, reportRepo.Object, auditLogger.Object, mediator.Object);

        await Assert.ThrowsAsync<PatientNotFoundException>(
            () => handler.Handle(new GenerateReportCommand(1, 1, ReportType.FullReport), CancellationToken.None));

        reportRepo.Verify(r => r.AddAsync(It.IsAny<Report>()), Times.Never);
    }
}
