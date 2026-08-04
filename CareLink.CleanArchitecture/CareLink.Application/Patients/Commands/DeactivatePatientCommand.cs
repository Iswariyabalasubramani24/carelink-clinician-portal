using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using MediatR;

namespace CareLink.Application.Patients.Commands;

// Soft-delete: a deactivated patient is excluded from the default patient
// list (the existing isActive search filter) but the record - and its
// history, reports, and notes - is retained, not destroyed.
public class DeactivatePatientCommand : IRequest<PatientDto>
{
    public int PatientId { get; set; }

    public int TenantId { get; set; }

    // The clinician performing the action (from JWT), not the patient.
    public int ClinicianId { get; set; }

    public DeactivatePatientCommand() { }

    public DeactivatePatientCommand(int patientId, int tenantId, int clinicianId = 0)
    {
        PatientId = patientId;
        TenantId = tenantId;
        ClinicianId = clinicianId;
    }
}

public class DeactivatePatientCommandHandler(IPatientRepository patientRepository, IAuditLogger auditLogger)
    : IRequestHandler<DeactivatePatientCommand, PatientDto>
{
    public async Task<PatientDto> Handle(DeactivatePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(request.PatientId, request.TenantId)
            ?? throw new PatientNotFoundException();

        patient.IsActive = false;
        patient.UpdatedAt = DateTime.UtcNow;
        await patientRepository.UpdateAsync(patient);

        await auditLogger.LogAsync(
            request.ClinicianId, request.TenantId, "PatientDeactivated", "Patient", patient.Id,
            $"MRN {patient.MedicalRecordNumber}");

        return PatientDto.FromEntity(patient);
    }
}
