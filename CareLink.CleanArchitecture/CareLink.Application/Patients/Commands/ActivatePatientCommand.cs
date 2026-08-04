using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using MediatR;

namespace CareLink.Application.Patients.Commands;

public class ActivatePatientCommand : IRequest<PatientDto>
{
    public int PatientId { get; set; }

    public int TenantId { get; set; }

    // The clinician performing the action (from JWT), not the patient.
    public int ClinicianId { get; set; }

    public ActivatePatientCommand() { }

    public ActivatePatientCommand(int patientId, int tenantId, int clinicianId = 0)
    {
        PatientId = patientId;
        TenantId = tenantId;
        ClinicianId = clinicianId;
    }
}

public class ActivatePatientCommandHandler(IPatientRepository patientRepository, IAuditLogger auditLogger)
    : IRequestHandler<ActivatePatientCommand, PatientDto>
{
    public async Task<PatientDto> Handle(ActivatePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(request.PatientId, request.TenantId)
            ?? throw new PatientNotFoundException();

        patient.IsActive = true;
        patient.UpdatedAt = DateTime.UtcNow;
        await patientRepository.UpdateAsync(patient);

        await auditLogger.LogAsync(
            request.ClinicianId, request.TenantId, "PatientActivated", "Patient", patient.Id,
            $"MRN {patient.MedicalRecordNumber}");

        return PatientDto.FromEntity(patient);
    }
}
