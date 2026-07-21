namespace CareLink.Application.PatientNotes;

public record PatientNoteDto(
    int Id,
    int PatientId,
    string ClinicianName,
    string Content,
    DateTime CreatedAt);
