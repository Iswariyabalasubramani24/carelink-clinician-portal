using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using MediatR;

namespace CareLink.Application.PatientNotes.Commands;

public class CreatePatientNoteCommand : IRequest<PatientNoteDto>
{
    public int PatientId { get; set; }

    public int TenantId { get; set; }

    public int ClinicianId { get; set; }

    public string Content { get; set; } = string.Empty;
}

public class CreatePatientNoteCommandHandler(
    IPatientRepository patientRepository,
    IPatientNoteRepository patientNoteRepository,
    IClinicianRepository clinicianRepository)
    : IRequestHandler<CreatePatientNoteCommand, PatientNoteDto>
{
    public async Task<PatientNoteDto> Handle(CreatePatientNoteCommand request, CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(request.PatientId, request.TenantId);
        if (patient is null)
        {
            throw new PatientNotFoundException();
        }

        var note = new PatientNote
        {
            PatientId = request.PatientId,
            TenantId = request.TenantId,
            ClinicianId = request.ClinicianId,
            Content = request.Content,
            CreatedAt = DateTime.UtcNow
        };

        var created = await patientNoteRepository.AddAsync(note);

        var clinician = await clinicianRepository.GetByIdAsync(request.ClinicianId);
        var clinicianName = clinician is null ? "Unknown Clinician" : $"{clinician.FirstName} {clinician.LastName}";

        return new PatientNoteDto(created.Id, created.PatientId, clinicianName, created.Content, created.CreatedAt);
    }
}
