using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using MediatR;

namespace CareLink.Application.PatientNotes.Queries;

public class GetPatientNotesQuery : IRequest<List<PatientNoteDto>>
{
    public int PatientId { get; set; }

    public int TenantId { get; set; }

    public GetPatientNotesQuery() { }

    public GetPatientNotesQuery(int patientId, int tenantId)
    {
        PatientId = patientId;
        TenantId = tenantId;
    }
}

public class GetPatientNotesQueryHandler(
    IPatientRepository patientRepository,
    IPatientNoteRepository patientNoteRepository,
    IClinicianRepository clinicianRepository)
    : IRequestHandler<GetPatientNotesQuery, List<PatientNoteDto>>
{
    public async Task<List<PatientNoteDto>> Handle(GetPatientNotesQuery request, CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(request.PatientId, request.TenantId);
        if (patient is null)
        {
            throw new PatientNotFoundException();
        }

        var notes = await patientNoteRepository.GetByPatientIdAsync(request.PatientId);

        var clinicianIds = notes.Select(n => n.ClinicianId).Distinct().ToList();
        var clinicians = await clinicianRepository.GetByIdsAsync(clinicianIds);
        var clinicianNamesById = clinicians.ToDictionary(c => c.Id, c => $"{c.FirstName} {c.LastName}");

        return notes.Select(n => new PatientNoteDto(
            n.Id,
            n.PatientId,
            clinicianNamesById.GetValueOrDefault(n.ClinicianId, "Unknown Clinician"),
            n.Content,
            n.CreatedAt)).ToList();
    }
}
