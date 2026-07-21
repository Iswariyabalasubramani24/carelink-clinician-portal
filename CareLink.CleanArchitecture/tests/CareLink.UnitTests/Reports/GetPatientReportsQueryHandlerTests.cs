using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using CareLink.Application.Reports.Queries;
using CareLink.Domain.Entities;
using Moq;

namespace CareLink.UnitTests.Reports;

public class GetPatientReportsQueryHandlerTests
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
    public async Task Handle_PatientBelongsToDifferentTenant_ThrowsPatientNotFoundException()
    {
        // The patient lookup is tenant-scoped, so a clinician authenticated
        // against a different tenant can never list another hospital's reports.
        var patientRepo = new Mock<IPatientRepository>();
        patientRepo.Setup(r => r.GetByIdAsync(1, 2)).ReturnsAsync((Patient?)null);
        var reportRepo = new Mock<IReportRepository>();

        var handler = new GetPatientReportsQueryHandler(patientRepo.Object, reportRepo.Object);

        await Assert.ThrowsAsync<PatientNotFoundException>(
            () => handler.Handle(new GetPatientReportsQuery(1, 2), CancellationToken.None));

        reportRepo.Verify(r => r.GetByPatientIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidPatient_ReturnsReportsForThatPatientOnly()
    {
        var patient = MakePatient(1, 1);
        var reports = new List<Report>
        {
            new() { Id = 1, PatientId = 1, TenantId = 1, ReportType = ReportType.FullReport, GeneratedAt = DateTime.UtcNow, DataSnapshot = "{}" }
        };

        var patientRepo = new Mock<IPatientRepository>();
        patientRepo.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(patient);
        var reportRepo = new Mock<IReportRepository>();
        reportRepo.Setup(r => r.GetByPatientIdAsync(1)).ReturnsAsync(reports);

        var handler = new GetPatientReportsQueryHandler(patientRepo.Object, reportRepo.Object);

        var result = await handler.Handle(new GetPatientReportsQuery(1, 1), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }
}
