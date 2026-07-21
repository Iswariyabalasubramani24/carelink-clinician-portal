using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using CareLink.Application.PatientNotes.Commands;
using CareLink.Domain.Entities;
using Moq;

namespace CareLink.UnitTests.PatientNotes;

public class CreatePatientNoteCommandHandlerTests
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

    private static Clinician MakeClinician(int id, int tenantId) => new()
    {
        Id = id,
        TenantId = tenantId,
        Email = "doctor@apollo.com",
        FirstName = "Anita",
        LastName = "Rao",
        Role = ClinicianRole.Clinician
    };

    [Fact]
    public async Task Handle_ValidRequest_CreatesNoteAndReturnsClinicianName()
    {
        var patient = MakePatient(1, 1);
        var clinician = MakeClinician(5, 1);

        var patientRepo = new Mock<IPatientRepository>();
        patientRepo.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(patient);

        var clinicianRepo = new Mock<IClinicianRepository>();
        clinicianRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(clinician);

        PatientNote? captured = null;
        var noteRepo = new Mock<IPatientNoteRepository>();
        noteRepo.Setup(r => r.AddAsync(It.IsAny<PatientNote>()))
            .Callback<PatientNote>(n => { n.Id = 100; captured = n; })
            .ReturnsAsync((PatientNote n) => n);

        var handler = new CreatePatientNoteCommandHandler(patientRepo.Object, noteRepo.Object, clinicianRepo.Object);

        var result = await handler.Handle(new CreatePatientNoteCommand
        {
            PatientId = 1,
            TenantId = 1,
            ClinicianId = 5,
            Content = "Patient is responding well to treatment."
        }, CancellationToken.None);

        Assert.Equal(100, result.Id);
        Assert.Equal("Anita Rao", result.ClinicianName);
        Assert.Equal("Patient is responding well to treatment.", result.Content);
        Assert.NotNull(captured);
        Assert.Equal(1, captured!.TenantId);
        Assert.Equal(5, captured.ClinicianId);
    }

    [Fact]
    public async Task Handle_PatientBelongsToDifferentTenant_ThrowsPatientNotFoundException()
    {
        // The patient lookup is tenant-scoped, so a clinician authenticated
        // against a different tenant can never post a note on this patient.
        var patientRepo = new Mock<IPatientRepository>();
        patientRepo.Setup(r => r.GetByIdAsync(1, 2)).ReturnsAsync((Patient?)null);

        var noteRepo = new Mock<IPatientNoteRepository>();
        var clinicianRepo = new Mock<IClinicianRepository>();

        var handler = new CreatePatientNoteCommandHandler(patientRepo.Object, noteRepo.Object, clinicianRepo.Object);

        await Assert.ThrowsAsync<PatientNotFoundException>(() => handler.Handle(new CreatePatientNoteCommand
        {
            PatientId = 1,
            TenantId = 2,
            ClinicianId = 5,
            Content = "Cross-tenant attempt"
        }, CancellationToken.None));

        noteRepo.Verify(r => r.AddAsync(It.IsAny<PatientNote>()), Times.Never);
    }
}
