using CareLink.Domain.Entities;

namespace CareLink.Application.Common.Interfaces;

public interface IPatientNoteRepository
{
    // Newest first - clinicians want to see the latest update on a patient
    // without scrolling, matching the Reports list convention.
    Task<List<PatientNote>> GetByPatientIdAsync(int patientId);

    Task<PatientNote> AddAsync(PatientNote note);
}
