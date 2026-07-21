using System.Text.Json;
using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using CareLink.Application.Reports;
using CareLink.Application.Reports.Queries;
using CareLink.Domain.Entities;
using Moq;

namespace CareLink.UnitTests.Reports;

public class DownloadReportQueryHandlerTests
{
    private static Report MakeReport(int id, int tenantId, int patientId = 1)
    {
        var snapshot = new ReportSnapshotDto(
            TenantName: "Apollo Hospital",
            PatientId: patientId,
            MedicalRecordNumber: $"MRN-{patientId}",
            FirstName: "Test",
            LastName: "Patient",
            DateOfBirth: new DateTime(1970, 1, 1),
            DeviceType: nameof(DeviceType.ICD),
            DeviceManufacturer: "Medtronic",
            DeviceModel: "Evera",
            DeviceSerialNumber: $"SN-{patientId}",
            ImplantDate: new DateTime(2020, 1, 1),
            BatteryLevel: 88.5m,
            LastHeartRate: 72,
            LastSyncedAt: DateTime.UtcNow,
            TransmissionHistory: [],
            Alerts: []);

        return new Report
        {
            Id = id,
            PatientId = patientId,
            TenantId = tenantId,
            ReportType = ReportType.FullReport,
            GeneratedAt = DateTime.UtcNow,
            DataSnapshot = JsonSerializer.Serialize(snapshot)
        };
    }

    [Fact]
    public async Task Handle_ReportNotFoundForTenant_ThrowsReportNotFoundException()
    {
        var reportRepo = new Mock<IReportRepository>();
        reportRepo.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync((Report?)null);
        var pdfGenerator = new Mock<IReportPdfGenerator>();

        var handler = new DownloadReportQueryHandler(reportRepo.Object, pdfGenerator.Object);

        await Assert.ThrowsAsync<ReportNotFoundException>(
            () => handler.Handle(new DownloadReportQuery(1, 1), CancellationToken.None));

        pdfGenerator.Verify(g => g.Generate(It.IsAny<ReportType>(), It.IsAny<DateTime>(), It.IsAny<ReportSnapshotDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReportBelongsToDifferentTenant_ThrowsReportNotFoundException()
    {
        // The repository is tenant-scoped, so a report generated under one hospital
        // must never be downloadable by a clinician authenticated against another.
        var reportRepo = new Mock<IReportRepository>();
        reportRepo.Setup(r => r.GetByIdAsync(1, 2)).ReturnsAsync((Report?)null);
        var pdfGenerator = new Mock<IReportPdfGenerator>();

        var handler = new DownloadReportQueryHandler(reportRepo.Object, pdfGenerator.Object);

        await Assert.ThrowsAsync<ReportNotFoundException>(
            () => handler.Handle(new DownloadReportQuery(1, 2), CancellationToken.None));

        reportRepo.Verify(r => r.GetByIdAsync(1, 2), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidReport_RegeneratesPdfFromStoredSnapshotAndReturnsFile()
    {
        var report = MakeReport(1, 1);

        var reportRepo = new Mock<IReportRepository>();
        reportRepo.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(report);

        var pdfBytes = new byte[] { 1, 2, 3 };
        var pdfGenerator = new Mock<IReportPdfGenerator>();
        pdfGenerator.Setup(g => g.Generate(ReportType.FullReport, report.GeneratedAt, It.IsAny<ReportSnapshotDto>()))
            .Returns(pdfBytes);

        var handler = new DownloadReportQueryHandler(reportRepo.Object, pdfGenerator.Object);

        var result = await handler.Handle(new DownloadReportQuery(1, 1), CancellationToken.None);

        Assert.Equal(pdfBytes, result.Content);
        Assert.EndsWith(".pdf", result.FileName);
        pdfGenerator.Verify(g => g.Generate(ReportType.FullReport, report.GeneratedAt, It.IsAny<ReportSnapshotDto>()), Times.Once);
    }
}
