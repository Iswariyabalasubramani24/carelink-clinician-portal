using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using CareLink.Application.PatientNotes.Queries;
using CareLink.Domain.Entities;
using Moq;

namespace CareLink.UnitTests.PatientNotes;

public class GetPatientNotesQueryHandlerTests
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

    private static Clinician MakeClinician(int id, string firstName, string lastName) => new()
    {
        Id = id,
        TenantId = 1,
        Email = $"clinician{id}@apollo.com",
        FirstName = firstName,
        LastName = lastName,
        Role = ClinicianRole.Clinician
    };

    [Fact]
    public async Task Handle_MultipleNotes_ReturnsThemNewestFirst()
    {
        var patient = MakePatient(1, 1);
        var oldest = new PatientNote { Id = 1, PatientId = 1, TenantId = 1, ClinicianId = 5, Content = "First note", CreatedAt = DateTime.UtcNow.AddDays(-3) };
        var newest = new PatientNote { Id = 2, PatientId = 1, TenantId = 1, ClinicianId = 5, Content = "Second note", CreatedAt = DateTime.UtcNow.AddDays(-1) };

        var patientRepo = new Mock<IPatientRepository>();
        patientRepo.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(patient);

        var noteRepo = new Mock<IPatientNoteRepository>();
        // The repository itself is responsible for ordering (newest first) -
        // the handler just needs to preserve whatever order it returns.
        noteRepo.Setup(r => r.GetByPatientIdAsync(1)).ReturnsAsync([newest, oldest]);

        var clinicianRepo = new Mock<IClinicianRepository>();
        clinicianRepo.Setup(r => r.GetByIdsAsync(It.Is<List<int>>(ids => ids.Contains(5))))
            .ReturnsAsync([MakeClinician(5, "Anita", "Rao")]);

        var handler = new GetPatientNotesQueryHandler(patientRepo.Object, noteRepo.Object, clinicianRepo.Object);

        var result = await handler.Handle(new GetPatientNotesQuery(1, 1), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("Second note", result[0].Content);
        Assert.Equal("First note", result[1].Content);
        Assert.All(result, dto => Assert.Equal("Anita Rao", dto.ClinicianName));
    }

    [Fact]
    public async Task Handle_PatientBelongsToDifferentTenant_ThrowsPatientNotFoundException()
    {
        // The patient lookup is tenant-scoped, so a clinician authenticated
        // against a different tenant can never read this patient's notes.
        var patientRepo = new Mock<IPatientRepository>();
        patientRepo.Setup(r => r.GetByIdAsync(1, 2)).ReturnsAsync((Patient?)null);

        var noteRepo = new Mock<IPatientNoteRepository>();
        var clinicianRepo = new Mock<IClinicianRepository>();

        var handler = new GetPatientNotesQueryHandler(patientRepo.Object, noteRepo.Object, clinicianRepo.Object);

        await Assert.ThrowsAsync<PatientNotFoundException>(
            () => handler.Handle(new GetPatientNotesQuery(1, 2), CancellationToken.None));

        noteRepo.Verify(r => r.GetByPatientIdAsync(It.IsAny<int>()), Times.Never);
    }
}
