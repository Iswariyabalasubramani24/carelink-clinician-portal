using CareLink.Application.Patients;
using CareLink.Application.Reports;
using CareLink.Domain.Entities;
using CareLink.Infrastructure.Reports;
using QuestPDF.Infrastructure;

namespace CareLink.UnitTests.Reports;

public class QuestPdfReportGeneratorTests
{
    static QuestPdfReportGeneratorTests()
    {
        // Document generation throws unless a license is selected; Program.cs
        // does this at startup, but tests run outside that host.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private static ReportSnapshotDto MakeSnapshot(
        List<TransmissionHistoryDto>? transmissionHistory = null,
        List<ReportAlertDto>? alerts = null) => new(
        TenantName: "Apollo Hospital",
        PatientId: 1,
        MedicalRecordNumber: "MRN-1",
        FirstName: "Test",
        LastName: "Patient",
        DateOfBirth: new DateTime(1970, 1, 1),
        DeviceType: nameof(DeviceType.ICD),
        DeviceManufacturer: "Medtronic",
        DeviceModel: "Evera",
        DeviceSerialNumber: "SN-1",
        ImplantDate: new DateTime(2020, 1, 1),
        BatteryLevel: 88.5m,
        LastHeartRate: 72,
        LastSyncedAt: DateTime.UtcNow,
        TransmissionHistory: transmissionHistory ?? [],
        Alerts: alerts ?? []);

    [Theory]
    [InlineData(ReportType.FullReport)]
    [InlineData(ReportType.SummaryReport)]
    [InlineData(ReportType.EventReport)]
    public void Generate_PatientWithNoAlertsAndNoTransmissionHistory_DoesNotThrowAndProducesPdfBytes(ReportType reportType)
    {
        var generator = new QuestPdfReportGenerator();
        var snapshot = MakeSnapshot();

        var bytes = generator.Generate(reportType, DateTime.UtcNow, snapshot);

        Assert.NotEmpty(bytes);
    }

    [Theory]
    [InlineData(ReportType.FullReport)]
    [InlineData(ReportType.SummaryReport)]
    [InlineData(ReportType.EventReport)]
    public void Generate_PatientWithFullData_DoesNotThrowAndProducesPdfBytes(ReportType reportType)
    {
        var generator = new QuestPdfReportGenerator();
        var snapshot = MakeSnapshot(
            transmissionHistory:
            [
                new TransmissionHistoryDto(DateTime.UtcNow.AddDays(-30), 70, 90m),
                new TransmissionHistoryDto(DateTime.UtcNow, 72, 88.5m)
            ],
            alerts:
            [
                new ReportAlertDto(nameof(AlertType.LowBattery), nameof(AlertUrgency.Yellow), DateTime.UtcNow.AddDays(-1), false, null),
                new ReportAlertDto(nameof(AlertType.IrregularHeartbeat), nameof(AlertUrgency.Red), DateTime.UtcNow.AddDays(-5), true, DateTime.UtcNow.AddDays(-4))
            ]);

        var bytes = generator.Generate(reportType, DateTime.UtcNow, snapshot);

        Assert.NotEmpty(bytes);
    }
}
